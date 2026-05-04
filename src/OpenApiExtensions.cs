using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace func;

public static class OpenApiFunctionsExtensions
{
    /// <summary>
    /// Registers OpenAPI document generation and wires up a transformer that discovers
    /// Azure Functions HTTP triggers, since they are not visible to the standard ASP.NET Core
    /// endpoint introspection used by <c>Microsoft.AspNetCore.OpenApi</c>.
    /// The document is served by <see cref="OpenApiFunction"/> at
    /// <c>/api/openapi/{documentName}.json</c>.
    /// </summary>
    public static IHostApplicationBuilder MapOpenApi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options =>
            options.AddDocumentTransformer(AddHttpTriggerPaths));
        return builder;
    }

    private static Task AddHttpTriggerPaths(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null) return Task.CompletedTask;

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance))
            {
                var functionAttr = method.GetCustomAttribute<FunctionAttribute>();
                if (functionAttr is null) continue;

                var httpTrigger = method.GetParameters()
                    .Select(p => p.GetCustomAttribute<HttpTriggerAttribute>())
                    .FirstOrDefault(a => a is not null);

                if (httpTrigger is null) continue;

                var route = httpTrigger.Route is not null
                    ? $"/{httpTrigger.Route.TrimStart('/')}"
                    : $"/api/{functionAttr.Name}";

                // Honour standard ASP.NET Core endpoint metadata attributes.
                var summary = method.GetCustomAttribute<EndpointSummaryAttribute>()?.Summary;
                var description = method.GetCustomAttribute<EndpointDescriptionAttribute>()?.Description;

                var pathItem = new OpenApiPathItem();

                foreach (var httpMethodStr in httpTrigger.Methods ?? [])
                {
                    pathItem.AddOperation(new HttpMethod(httpMethodStr.ToUpperInvariant()), new OpenApiOperation
                    {
                        Summary = summary,
                        Description = description,
                        Responses = new OpenApiResponses
                        {
                            { "200", new OpenApiResponse { Description = "Success" } }
                        }
                    });
                }

                document.Paths ??= new OpenApiPaths();
                document.Paths[route] = pathItem;
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Accesses the OpenAPI document via <c>IDocumentProvider</c> without a compile-time reference
/// to the assembly that defines it. We reach the interface type through the concrete
/// <c>OpenApiDocumentProvider</c> implementation that <c>AddOpenApi()</c> registers, both of
/// which live in <c>Microsoft.AspNetCore.OpenApi.dll</c>.
/// </summary>
internal static class OpenApiDocumentAccessor
{
    private static readonly Lazy<ResolvedProvider> Resolved = new(Resolve);

    private sealed record ResolvedProvider(Type? InterfaceType, MethodInfo? GenerateAsync);

    private static ResolvedProvider Resolve()
    {
        var openApiAssembly = typeof(Microsoft.AspNetCore.Builder.OpenApiEndpointRouteBuilderExtensions).Assembly;
        var concreteType = openApiAssembly.GetType(
            "Microsoft.Extensions.ApiDescriptions.OpenApiDocumentProvider");

        var interfaceType = concreteType?
            .GetInterfaces()
            .FirstOrDefault(i => i.Name == "IDocumentProvider");

        var generateMethod = interfaceType?
            .GetMethods()
            .Where(m => m.Name == "GenerateAsync")
            .OrderBy(m => m.GetParameters().Length)
            .FirstOrDefault();

        return new(interfaceType, generateMethod);
    }

    public static async Task<string?> GenerateAsync(
        IServiceProvider services,
        string documentName,
        CancellationToken cancellationToken = default)
    {
        var (interfaceType, generateMethod) = Resolved.Value;
        if (interfaceType is null || generateMethod is null)
            return null;

        var provider = services.GetService(interfaceType);
        if (provider is null)
            return null;

        var getNamesMethod = interfaceType.GetMethod("GetDocumentNames");
        if (getNamesMethod?.Invoke(provider, []) is IReadOnlyList<string> names &&
            !names.Any(n => string.Equals(n, documentName, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        await using var sw = new StringWriter();

        object[] args = generateMethod.GetParameters().Length == 3
            ? [documentName, sw, cancellationToken]
            : [documentName, sw];

        var task = (Task)generateMethod.Invoke(provider, args)!;
        await task;
        return sw.ToString();
    }
}
