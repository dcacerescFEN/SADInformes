using WebApi.Extension;

namespace WebApi.Endpoints.Config;

public sealed class PublicKeycloakConfigEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/config/keycloak", GetKeycloakConfiguration)
            .AllowAnonymous().WithTags("Config").WithSummary("Obtiene la configuración pública de Keycloak");

    private static IResult GetKeycloakConfiguration(IConfiguration configuration) => Results.Ok(new
    {
        url = configuration["Keycloak:Url"]?.TrimEnd('/'),
        realm = configuration["Keycloak:Realm"],
        clientId = configuration["Keycloak:ClientId"],
    });
}
