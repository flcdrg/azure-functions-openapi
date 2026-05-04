using Gardiner.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace tests;

public class OpenApiFunctionsExtensionsTests
{
    [Fact]
    public void MapOpenApi_WhenCalled_ReturnsBuilder()
    {
        // Arrange
        var builder = TestBuilderHelper.CreateBuilder();

        // Act
        var result = builder.MapOpenApi();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void MapOpenApi_WhenCalled_CallsAddOpenApiOnServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = TestBuilderHelper.CreateBuilder(services);

        // Act
        builder.MapOpenApi();

        // Assert
        Assert.Contains(services, sd => 
            sd.ServiceType.FullName != null && 
            sd.ServiceType.FullName.Contains("OpenApi"));
    }

    [Fact]
    public void MapOpenApi_WhenCalledMultipleTimes_ReturnsBuilderEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = TestBuilderHelper.CreateBuilder(services);

        // Act
        var result1 = builder.MapOpenApi();
        var result2 = builder.MapOpenApi();

        // Assert
        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
    }

    [Fact]
    public void MapOpenApi_WhenCalled_RegistersOpenApiServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = TestBuilderHelper.CreateBuilder(services);

        // Act
        builder.MapOpenApi();

        // Assert
        Assert.NotEmpty(services);
    }
}
