using Application.Common.Mediator;
using Application.Config.Queries.GetSiteConfig;
using WebApi.Extension;

namespace WebApi.Endpoints.Config;

public sealed class ConfigEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config").RequireAuthorization().WithTags("Config");
        group.MapGet("/site", GetSiteConfig)
            .RequireAuthorization(Domain.Constants.Permission.REPORT_ACCESS)
            .WithSummary("Obtiene la configuración inicial del sitio");
    }

    private static async Task<IResult> GetSiteConfig(IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(new GetSiteConfig(), cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(statusCode: 503, title: "La navegación no está disponible.");
        }
    }
}
