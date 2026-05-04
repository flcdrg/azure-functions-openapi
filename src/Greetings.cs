using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;

namespace func;

// ── Response / request models ────────────────────────────────────────────────

/// <summary>
/// Response payload for greeting operations.
/// </summary>
public record GreetingResponse(
    [property: Description("The personalised greeting message.")]
    string Message,

    [property: Description("The BCP-47 language code used.")]
    [property: RegularExpression(@"^[a-z]{2}$")]
    string Lang
);

/// <summary>
/// Request payload for creating or updating a greeting.
/// </summary>
public record GreetingRequest(
    [property: Required]
    [property: MinLength(1), MaxLength(50)]
    [property: Description("The name to greet.")]
    string Name,

    [property: DefaultValue("en")]
    [property: Description("BCP-47 language code. Supported: en, es, fr.")]
    string? Lang
);

/// <summary>
/// Error details when language is not supported.
/// </summary>
public record LanguageNotSupportedError(
    [property: Description("Error message describing the unsupported language.")]
    string Detail
);

// ── Functions ────────────────────────────────────────────────────────────────

public static class Greetings
{
    private static readonly Dictionary<string, string> Phrases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Hello",
            ["es"] = "Hola",
            ["fr"] = "Bonjour",
        };

    /// <summary>
    /// GET /api/greetings/{name}?lang=en
    ///
    /// Demonstrates: [EndpointSummary], [EndpointDescription], [Tags], [EndpointName],
    ///               [ProducesResponseType&lt;T&gt;] with Description, route + query params.
    /// </summary>
    [EndpointSummary("Get a greeting")]
    [EndpointDescription("Returns a personalised greeting for *name* in the requested language.")]
    [Tags(["greetings", "retrieval"])]
    [EndpointName("GetGreeting")]
    [ProducesResponseType<GreetingResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json",
        Description = "The requested language is not supported.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json",
        Description = "The name parameter is invalid or missing.")]
    [Function("GetGreeting")]
    public static IActionResult Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "greetings/{name}")]
        HttpRequest req,
        [Description("The name to include in the greeting (1-50 characters).")]
        string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            return new BadRequestObjectResult(
                new ProblemDetails
                {
                    Title = "Invalid Name",
                    Detail = "Name must be 1-50 characters."
                });
        }

        // Query parameters aren't auto-bound in Functions — read from HttpRequest.
        var lang = (string?)req.Query["lang"] ?? "en";

        if (!Phrases.TryGetValue(lang, out var phrase))
        {
            return new NotFoundObjectResult(
                new ProblemDetails
                {
                    Title = "Language Not Supported",
                    Detail = $"Language '{lang}' is not supported. Supported: en, es, fr."
                });
        }

        return new OkObjectResult(new GreetingResponse($"{phrase}, {name}!", lang));
    }

    /// <summary>
    /// POST /api/greetings
    ///
    /// Demonstrates: [Consumes], [ProducesResponseType] with multiple status codes,
    ///               [FromBody] implicit binding with request model.
    /// </summary>
    [EndpointSummary("Create a greeting")]
    [EndpointDescription("Returns a greeting for the name and language provided in the request body.")]
    [Tags(["greetings", "creation"])]
    [EndpointName("CreateGreeting")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<GreetingResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest,
        "application/problem+json", Description = "The request body failed validation or unsupported language.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity,
        "application/problem+json", Description = "The request body is malformed or missing required fields.")]
    [Function("CreateGreeting")]
    public static async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "greetings")]
        HttpRequest req)
    {
        GreetingRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<GreetingRequest>();
        }
        catch
        {
            return new UnprocessableEntityObjectResult(new ProblemDetails
            {
                Title = "Invalid JSON",
                Detail = "Request body must be valid JSON matching GreetingRequest."
            });
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Title = "Missing Required Field",
                Detail = "'name' is required and must not be empty."
            });
        }

        var lang = body.Lang ?? "en";

        if (!Phrases.TryGetValue(lang, out var phrase))
        {
            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Title = "Unsupported Language",
                Detail = $"Language '{lang}' is not supported. Supported: en, es, fr."
            });
        }

        return new OkObjectResult(new GreetingResponse($"{phrase}, {body.Name}!", lang));
    }

    /// <summary>
    /// PUT /api/greetings/{name}
    ///
    /// Demonstrates: [Consumes], request body binding with route parameter,
    ///               [ProducesResponseType] for update operation with multiple outcomes.
    /// </summary>
    [EndpointSummary("Update a greeting's language")]
    [EndpointDescription("Updates the language preference for an existing greeting and returns the updated greeting.")]
    [Tags(["greetings", "modification"])]
    [EndpointName("UpdateGreeting")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<GreetingResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest,
        "application/problem+json", Description = "The request body or parameters are invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,
        "application/problem+json", Description = "The requested language is not supported.")]
    [Function("UpdateGreeting")]
    public static async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "greetings/{name}")]
        HttpRequest req,
        [Description("The name of the person whose greeting to update.")]
        string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            return new BadRequestObjectResult(
                new ValidationProblemDetails
                {
                    Title = "Invalid Name",
                    Detail = "Name must be 1-50 characters."
                });
        }

        GreetingRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<GreetingRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Title = "Invalid JSON",
                Detail = "Request body must be valid JSON."
            });
        }

        if (body is null)
        {
            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Title = "Empty Request Body",
                Detail = "Request body is required."
            });
        }

        var lang = body.Lang ?? "en";

        if (!Phrases.TryGetValue(lang, out var phrase))
        {
            return new NotFoundObjectResult(new ProblemDetails
            {
                Title = "Language Not Supported",
                Detail = $"Language '{lang}' is not supported. Supported: en, es, fr."
            });
        }

        return new OkObjectResult(new GreetingResponse($"{phrase}, {name}!", lang));
    }

    /// <summary>
    /// DELETE /api/greetings/{name}
    ///
    /// Demonstrates: [ProducesResponseType] for successful deletion (204 No Content),
    ///               query parameter usage, [Tags] for operation categorization.
    /// </summary>
    [EndpointSummary("Delete a greeting")]
    [EndpointDescription("Removes a greeting from the system. Returns 204 No Content on success.")]
    [Tags(["greetings", "deletion"])]
    [EndpointName("DeleteGreeting")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Description = "The greeting was successfully deleted.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,
        "application/problem+json", Description = "The name parameter is invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,
        "application/problem+json", Description = "The greeting does not exist.")]
    [Function("DeleteGreeting")]
    public static IActionResult Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "greetings/{name}")]
        HttpRequest req,
        [Description("The name of the greeting to delete (1-50 characters).")]
        string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            return new BadRequestObjectResult(
                new ProblemDetails
                {
                    Title = "Invalid Name",
                    Detail = "Name must be 1-50 characters."
                });
        }

        // In a real application, you would delete from storage here
        // For this demo, we simulate success
        return new NoContentResult();
    }

    /// <summary>
    /// [DEPRECATED] Use GET /api/greetings/{name} instead.
    ///
    /// A private admin endpoint excluded from the OpenAPI document via
    /// [ApiExplorerSettings(IgnoreApi = true)].
    /// </summary>
    [EndpointSummary("[DEPRECATED] Legacy greeting endpoint")]
    [EndpointDescription("This endpoint is deprecated and will be removed in a future version. Use GET /api/greetings/{name} instead.")]
    [Obsolete("Use GetGreeting endpoint instead.", false)]
    [Tags(["greetings", "legacy"])]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Function("GreetingsInternal")]
    public static IActionResult Internal(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "greetings/internal")]
        HttpRequest req)
        => new OkObjectResult("internal");
}
