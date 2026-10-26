using Application.Configuration;
using Application.Navigation;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAppConfigurationReader>(_ => new AppConfigConfigurationReader(
            () => new AppConfigContext(new DbContextOptionsBuilder<AppConfigContext>()
                .UseSqlServer(configuration.GetConnectionString("AppConfig"),
                    options => options.CommandTimeout(10)).Options)));
        services.AddScoped<ConfigurationInitialiser>();
        services.AddSingleton<SiteSettings>();
        services.AddSingleton<ISiteMenuProvider, SiteMenuProvider>();
        return services;
    }
}
