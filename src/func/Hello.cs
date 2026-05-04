using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace src;

public class Hello
{
    private readonly ILogger<Hello> _logger;

    public Hello(ILogger<Hello> logger)
    {
        _logger = logger;
    }

    [Tags(["todos", "projects"])]
    [EndpointSummary("This is a summary.")]
    [EndpointDescription("This is a description.")]
    [EndpointName("FromHello")]
    [Function("Hello")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, [Description("This is a description.")] string name)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
