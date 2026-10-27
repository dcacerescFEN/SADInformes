using Microsoft.AspNetCore.Authorization;
using WebApi.Services;
using WebApi.Authentication;
using WebApi.OpenApi;

namespace WebApi;

public static class ConfigureServices
{
    public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeycloakAuthentication(configuration);
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddApiDocumentation();
        return services;
    }
}
