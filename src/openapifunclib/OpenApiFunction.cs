using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Gardiner.Azure.Functions.Worker.Extensions.OpenApi;

public static class OpenApiFunction
{
    [Function("OpenApiDocument")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi/{documentName}.json")]
        HttpRequest req,
        string documentName)
    {
        var lowerName = documentName.ToLowerInvariant();

        var json = await OpenApiDocumentAccessor.GenerateAsync(
            req.HttpContext.RequestServices, lowerName, req.HttpContext.RequestAborted);

        if (json is null)
        {
            return new NotFoundObjectResult(
                $"No OpenAPI document with the name '{lowerName}' was found.");
        }

        return new ContentResult
        {
            Content = json,
            ContentType = "application/json;charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
