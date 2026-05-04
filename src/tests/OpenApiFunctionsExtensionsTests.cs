using Gardiner.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace tests;

public class OpenApiFunctionsExtensionsTests
{
    [Fact]
    public void MapOpenApi_WhenCalled_ReturnsBuilder()
    {
        // Arrange
        var mockServices = new Mock<IServiceCollection>();
        var mockBuilder = new Mock<IHostApplicationBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(mockServices.Object);

        // Act
        var result = mockBuilder.Object.MapOpenApi();

        // Assert
        Assert.Same(mockBuilder.Object, result);
    }

    [Fact]
    public void MapOpenApi_WhenCalled_CallsAddOpenApiOnServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IHostApplicationBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        mockBuilder.Object.MapOpenApi();

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
        var mockBuilder = new Mock<IHostApplicationBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var result1 = mockBuilder.Object.MapOpenApi();
        var result2 = mockBuilder.Object.MapOpenApi();

        // Assert
        Assert.Same(mockBuilder.Object, result1);
        Assert.Same(mockBuilder.Object, result2);
    }

    [Fact]
    public void MapOpenApi_WhenCalled_RegistersOpenApiServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IHostApplicationBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        mockBuilder.Object.MapOpenApi();

        // Assert
        Assert.NotEmpty(services);
    }
}
