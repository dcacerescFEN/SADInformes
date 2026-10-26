using Application.Navigation;
using WebApi.Extension;

namespace WebApi.Endpoints.Config;

public sealed class ConfigEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config").RequireAuthorization().WithTags("Config");
        group.MapGet("/site", GetSiteConfig).WithSummary("Obtiene la configuración inicial del sitio");
    }

    private static async Task<IResult> GetSiteConfig(ISiteMenuProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(new { menu = await provider.GetMenuAsync(cancellationToken) });
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(statusCode: 503, title: "La navegación no está disponible.");
        }
    }
}
