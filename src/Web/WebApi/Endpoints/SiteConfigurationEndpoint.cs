using System.Data.Common;
using Application.Navigation;

namespace WebApi.Endpoints;

public static class SiteConfigurationEndpoint
{
    public static void MapSiteConfiguration(this WebApplication app) =>
        app.MapGet("/api/config/site", async (ISiteMenuProvider provider, CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(new { menu = await provider.GetMenuAsync(cancellationToken) });
            }
            catch (Exception error) when (error is DbException or InvalidOperationException or ArgumentException)
            {
                return Results.Problem(statusCode: 503, title: "La navegación no está disponible.");
            }
        }).RequireAuthorization();
}
