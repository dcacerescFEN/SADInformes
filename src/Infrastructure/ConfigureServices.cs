using Application.Navigation;
using Application.Common.Interfaces;
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
        services.AddDbContext<AppConfigContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AppConfig"),
                sql => sql.CommandTimeout(10)));
        services.AddScoped<IApiResponseService, ApiResponseService>();
        services.AddScoped<ConfigurationInitialiser>();
        services.AddSingleton<SiteSettings>();
        services.AddSingleton<ISiteMenuProvider, SiteMenuProvider>();
        return services;
    }
}
