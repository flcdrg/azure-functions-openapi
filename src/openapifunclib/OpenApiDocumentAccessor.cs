using System.Reflection;

namespace Gardiner.Azure.Functions.Worker.Extensions.OpenApi;

/// <summary>
/// Accesses the OpenAPI document via <c>IDocumentProvider</c> without a compile-time reference
/// to the assembly that defines it. We reach the interface type through the concrete
/// <c>OpenApiDocumentProvider</c> implementation that <c>AddOpenApi()</c> registers, both of
/// which live in <c>Microsoft.AspNetCore.OpenApi.dll</c>.
/// </summary>
public static class OpenApiDocumentAccessor
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