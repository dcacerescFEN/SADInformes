using Application.Navigation;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Infrastructure.Reporting;
using System.Net;
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
        services.AddDbContext<SadContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Sad"),
                sql => sql.CommandTimeout(60)));
        services.AddDbContext<SigmaRemuneracionesContext>(options =>
            options.UseMySQL(configuration.GetConnectionString("SigmaRemuneraciones")!));


        services.AddScoped<ISadContext>(provider => provider.GetRequiredService<SadContext>());
        services.AddScoped<ISadContextProcedures, SadContextProcedures>();
        services.AddScoped<ISigmaRemuneracionesContext>(provider => provider.GetRequiredService<SigmaRemuneracionesContext>());

        services.AddScoped<ConfigurationInitialiser>();
        services.AddSingleton<SiteSettings>();
        services.AddSingleton<PostgraduateSettings>();
        services.AddSingleton<ReportingSettings>();
        services.AddScoped<IReportRenderer, SsrsReportRenderer>();
        services.AddHttpClient(SsrsReportRenderer.ClientName, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var settings = provider.GetRequiredService<ReportingSettings>();
                return new HttpClientHandler
                {
                    Credentials = new NetworkCredential(settings.Username, settings.Password, settings.Domain),
                    PreAuthenticate = true,
                    AllowAutoRedirect = false,
                    UseCookies = false,
                };
            });
        services.AddSingleton<IPostgraduateSettings>(
            provider => provider.GetRequiredService<PostgraduateSettings>());
        services.AddSingleton<ISiteMenuProvider, SiteMenuProvider>();
        return services;
    }
}
