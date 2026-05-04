using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace tests;

/// <summary>
/// Test helper for creating a configured IHostApplicationBuilder substitute.
/// Uses NSubstitute for complex interface types that are not domain logic.
/// </summary>
public static class TestBuilderHelper
{
    /// <summary>
    /// Creates an IHostApplicationBuilder substitute with a real service collection
    /// and required hosting services.
    /// </summary>
    public static IHostApplicationBuilder CreateBuilder(
        IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        
        // Add required services for OpenAPI document generation
        if (services.All(s => s.ServiceType != typeof(IHostEnvironment)))
        {
            var hostEnv = Substitute.For<IHostEnvironment>();
            hostEnv.EnvironmentName.Returns("Development");
            hostEnv.ApplicationName.Returns("TestApp");
            services.AddSingleton(hostEnv);
        }

        var builder = Substitute.For<IHostApplicationBuilder>();
        builder.Services.Returns(services);
        return builder;
    }
}
