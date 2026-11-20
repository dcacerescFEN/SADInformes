using Application.Common.Mediator;
using Application.Reports.Regulation.ExpedientesNoAbiertos.Queries.GetUnopenedFiles;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Regulation;

public sealed class UnopenedFilesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/regulation/unopened-files")
            .RequireAuthorization(Permission.REPORT_ACCESS, Permission.REPORT_VIEW_REGULATION)
            .WithTags("Reports/Regulation");
        group.MapGet("/report", GetReport)
            .WithSummary("Alumnos con expediente pendiente de apertura");
    }

    private static async Task<IResult> GetReport(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(new GetUnopenedFiles(), cancellationToken));
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
