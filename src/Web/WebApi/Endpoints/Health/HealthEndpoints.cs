using WebApi.Extension;

namespace WebApi.Endpoints.Health;

public sealed class HealthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", application = "SADInformes" }))
            .AllowAnonymous().WithTags("Health");
}
