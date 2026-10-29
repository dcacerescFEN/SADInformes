using Infrastructure;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ConfigurationRow = Domain.Entities.AppConfig.Configuration;

namespace WebApi.Tests;

public sealed class PostgraduateSettingsTests
{
    [Fact]
    public async Task Active_user_is_loaded_trimmed_from_the_selected_application_and_environment()
    {
        using var services = CreateServices(
            Row("postgraduateuser", "  report-user  "),
            Row("PostgraduateUser", "other-app", application: "SADAlumnos"),
            Row("PostgraduateUser", "other-env", environment: "PROD"),
            Row("PostgraduateUser", "inactive", active: false));
        await Initialise(services);
        Assert.Equal("report-user", services.GetRequiredService<PostgraduateSettings>().User);
        Assert.Equal("SADInformes", services.GetRequiredService<SiteSettings>().ApplicationName);
        Assert.DoesNotContain(services.GetRequiredService<SiteSettings>().Menu.Keys,
            key => key.Contains("postgraduate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task User_accepts_the_procedure_parameter_length_boundary()
    {
        using var services = CreateServices(Row("PostgraduateUser", "abcdefghijklmnopqrst"));
        await Initialise(services);
        Assert.Equal("abcdefghijklmnopqrst", services.GetRequiredService<PostgraduateSettings>().User);
    }

    [Fact]
    public async Task Missing_user_uses_mvenegas_as_the_default()
    {
        using var services = CreateServices();
        await Initialise(services);
        Assert.Equal("mvenegas", services.GetRequiredService<PostgraduateSettings>().User);
        Assert.Equal("SADInformes", services.GetRequiredService<SiteSettings>().ApplicationName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcdefghijklmnopqrstu")]
    public async Task Invalid_user_is_unavailable_without_disabling_navigation(string user)
    {
        using var services = CreateServices(Row("PostgraduateUser", user));
        await Initialise(services);
        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<PostgraduateSettings>().User);
        Assert.Equal("https://sadinformes-dev.fen.uchile.cl",
            services.GetRequiredService<SiteSettings>().Menu["reportsMenuUrl"]);
    }

    [Fact]
    public async Task Initialisation_failure_does_not_publish_a_fallback_user()
    {
        using var services = CreateServices();
        await Assert.ThrowsAsync<InvalidOperationException>(() => services
            .GetRequiredService<ConfigurationInitialiser>().InitialiseAsync(new ConfigurationBuilder().Build()));
        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<PostgraduateSettings>().User);
    }

    private static ServiceProvider CreateServices(params ConfigurationRow[] rows)
    {
        var configuration = Configuration();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);
        var options = new DbContextOptionsBuilder<AppConfigContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        services.AddScoped(_ => new AppConfigContext(options));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppConfigContext>();
        context.Configurations.AddRange(rows);
        context.SaveChanges();
        return provider;
    }

    private static Task Initialise(ServiceProvider services) => services
        .GetRequiredService<ConfigurationInitialiser>().InitialiseAsync(Configuration());

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            ["ConfigureSite:ApplicationName"] = "SADInformes",
            ["ConfigureSite:EnvironmentName"] = "DEV",
        }).Build();

    private static ConfigurationRow Row(string key, string value, string application = "SADInformes",
        string environment = "DEV", bool active = true) => new()
    {
        ConfigurationName = key, ConfigurationValue = value, IsActive = active,
        Application = new() { ApplicationName = application, IsActive = true },
        Environment = new() { EnvironmentName = environment, IsActive = true },
    };
}
