using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace func;

// ── Response / request models ────────────────────────────────────────────────

public record GreetingResponse(
    [property: Description("The personalised greeting message.")]
    string Message,

    [property: Description("The BCP-47 language code used.")]
    [property: RegularExpression(@"^[a-z]{2}$")]
    string Lang
);

public record GreetingRequest(
    [property: Required]
    [property: MinLength(1), MaxLength(50)]
    [property: Description("The name to greet.")]
    string Name,

    [property: DefaultValue("en")]
    [property: Description("BCP-47 language code. Supported: en, es, fr.")]
    string? Lang
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
    [Tags(["greetings"])]
    [EndpointName("GetGreeting")]
    [ProducesResponseType<GreetingResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status404NotFound,
        Description = "The requested language is not supported.")]
    [Function("GetGreeting")]
    public static IActionResult Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "greetings/{name}")]
        HttpRequest req,
        [Description("The name to include in the greeting.")]
        string name)
    {
        // Query parameters aren't auto-bound in Functions — read from HttpRequest.
        // Document them with [OpenApiQueryParam] via a custom attribute (see transformer).
        var lang = (string?)req.Query["lang"] ?? "en";

        if (!Phrases.TryGetValue(lang, out var phrase))
        {
            return new NotFoundObjectResult(
                new ProblemDetails { Detail = $"Language '{lang}' is not supported." });
        }

        return new OkObjectResult(new GreetingResponse($"{phrase}, {name}!", lang));
    }

    /// <summary>
    /// POST /api/greetings
    ///
    /// Demonstrates: [Consumes], [ProducesResponseType] with multiple status codes,
    ///               [ApiExplorerSettings] applied at method level to show it being honoured.
    /// </summary>
    [EndpointSummary("Create a greeting")]
    [EndpointDescription("Returns a greeting for the name and language in the request body.")]
    [Tags(["greetings"])]
    [EndpointName("CreateGreeting")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<GreetingResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest,
        "application/problem+json", Description = "The request body failed validation.")]
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
            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Detail = "Request body must be valid JSON matching GreetingRequest."
            });
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Detail = "'name' is required."
            });
        }

        var lang = body.Lang ?? "en";

        if (!Phrases.TryGetValue(lang, out var phrase))
        {
            return new BadRequestObjectResult(new ValidationProblemDetails
            {
                Detail = $"Language '{lang}' is not supported. Use: en, es, fr."
            });
        }

        return new OkObjectResult(new GreetingResponse($"{phrase}, {body.Name}!", lang));
    }

    /// <summary>
    /// A private admin endpoint excluded from the OpenAPI document via
    /// [ApiExplorerSettings(IgnoreApi = true)].
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Function("GreetingsInternal")]
    public static IActionResult Internal(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "greetings/internal")]
        HttpRequest req)
        => new OkObjectResult("internal");
}
