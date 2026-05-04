using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace Gardiner.Azure.Functions.Worker.Extensions.OpenApi;

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

    private static readonly Regex RouteParamRegex =
        new(@"\{([^:}]+)(?::[^}]*)?\}", RegexOptions.Compiled);

    private static Task AddHttpTriggerPaths(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null) return Task.CompletedTask;

        document.Paths ??= new OpenApiPaths();
        var tagList = new List<OpenApiTag>();

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance))
            {
                var functionAttr = method.GetCustomAttribute<FunctionAttribute>();
                if (functionAttr is null) continue;

                // Honour [ApiExplorerSettings(IgnoreApi = true)]
                var explorerSettings = method.GetCustomAttribute<ApiExplorerSettingsAttribute>();
                if (explorerSettings?.IgnoreApi == true) continue;

                var httpTrigger = method.GetParameters()
                    .Select(p => p.GetCustomAttribute<HttpTriggerAttribute>())
                    .FirstOrDefault(a => a is not null);

                if (httpTrigger is null) continue;

                var route = httpTrigger.Route is not null
                    ? $"/{httpTrigger.Route.TrimStart('/')}"
                    : $"/api/{functionAttr.Name}";

                // Collect endpoint metadata attributes
                var summary = method.GetCustomAttribute<EndpointSummaryAttribute>()?.Summary;
                var description = method.GetCustomAttribute<EndpointDescriptionAttribute>()?.Description;
                var endpointName = method.GetCustomAttribute<EndpointNameAttribute>()?.EndpointName;
                var tags = method.GetCustomAttribute<TagsAttribute>()?.Tags;
                var consumesAttr = method.GetCustomAttribute<ConsumesAttribute>();
                var producesAttrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();
                var deprecatedAttr = method.GetCustomAttribute<ObsoleteAttribute>();

                // Register tags in document
                if (tags is not null)
                {
                    foreach (var tag in tags)
                    {
                        if (tagList.All(t => t.Name != tag))
                        {
                            tagList.Add(new OpenApiTag { Name = tag });
                        }
                    }
                }

                // Extract {paramName} and {paramName:constraint} placeholders from the route template
                var routeParams = RouteParamRegex.Matches(route)
                    .Select(m => m.Groups[1].Value)
                    .ToList();

                var pathItem = new OpenApiPathItem();

                foreach (var httpMethodStr in httpTrigger.Methods ?? [])
                {
                    var responses = BuildResponses(producesAttrs, assembly);

                    // Build parameters including route parameters with descriptions
                    var parameters = BuildParameters(method, routeParams);

                    var operation = new OpenApiOperation
                    {
                        Summary = summary,
                        Description = description,
                        OperationId = endpointName,
                        Responses = responses,
                        Deprecated = deprecatedAttr is not null,
                        Parameters = parameters,
                    };

                    // Add tags to operation as tag references
                    if (tags is not null && tags.Any())
                    {
                        operation.Tags = new HashSet<OpenApiTagReference>(
                            tags.Select(t => new OpenApiTagReference(t, document))
                        );
                    }

                    // Add request body
                    if (consumesAttr is not null)
                    {
                        operation.RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = consumesAttr.ContentTypes
                                .ToDictionary(ct => ct, _ => new OpenApiMediaType()),
                        };
                    }

                    pathItem.AddOperation(new HttpMethod(httpMethodStr.ToUpperInvariant()), operation);
                }

                document.Paths[route] = pathItem;
            }
        }

        // Add collected tags to the document
        if (tagList.Any())
        {
            document.Tags = new HashSet<OpenApiTag>(tagList);
        }

        return Task.CompletedTask;
    }

    private static List<IOpenApiParameter> BuildParameters(MethodInfo method, List<string> routeParams)
    {
        var parameters = new List<IOpenApiParameter>();

        // Add route parameters with descriptions from method parameters
        var methodParams = method.GetParameters();
        foreach (var routeParam in routeParams)
        {
            var methodParam = methodParams.FirstOrDefault(p => 
                p.Name?.Equals(routeParam, StringComparison.OrdinalIgnoreCase) == true &&
                p.GetCustomAttribute<HttpTriggerAttribute>() is null);

            var description = methodParam?.GetCustomAttribute<DescriptionAttribute>()?.Description;

            parameters.Add(new OpenApiParameter
            {
                Name = routeParam,
                In = ParameterLocation.Path,
                Required = true,
                Description = description
            });
        }

        return parameters;
    }

    private static OpenApiResponses BuildResponses(
        List<ProducesResponseTypeAttribute> attrs,
        Assembly assembly)
    {
        var responses = new OpenApiResponses();

        if (attrs.Count == 0)
        {
            responses["200"] = new OpenApiResponse { Description = "Success" };
            return responses;
        }

        foreach (var attr in attrs)
        {
            var statusCode = attr.StatusCode.ToString();
            var description = attr.Description ?? DefaultDescription(attr.StatusCode);

            var response = new OpenApiResponse { Description = description };
            responses[statusCode] = response;
        }

        return responses;
    }

    private static string DefaultDescription(int statusCode) => statusCode switch
    {
        200 => "Success",
        201 => "Created",
        204 => "No Content",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        _ => statusCode.ToString(),
    };
}