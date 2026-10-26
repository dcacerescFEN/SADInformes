using WebApi.Authentication;
using WebApi.Extension;

namespace WebApi.Endpoints.Config;

public sealed class PublicKeycloakConfigEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/config/keycloak", (PublicKeycloakConfiguration configuration) => Results.Ok(configuration))
            .AllowAnonymous().WithTags("Config");
}
