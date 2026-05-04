using Gardiner.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace tests;

public class OpenApiDocumentSnapshotTests
{
    [Fact]
    public async Task GenerateOpenApiDocument_ProducesValidJsonDocument_Snapshot()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddOpenApi();
        var app = builder.Build();

        // Act
        var jsonDocument = await OpenApiDocumentAccessor.GenerateAsync(
            app.Services, "v1", CancellationToken.None);

        // Assert
        await Verify(jsonDocument)
            .DontScrubGuids();
    }
}
