using Application.Common.Mediator;
using Application.Reports.Regulation.ExpedientesXPeriodo.Queries.GetOpenedFilesByPeriod;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Regulation;

public sealed class OpenedFilesByPeriodEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/regulation/opened-files-by-period")
            .RequireAuthorization(Permission.REPORT_ACCESS, Permission.REPORT_VIEW_REGULATION)
            .WithTags("Reports/Regulation");
        group.MapGet("/report", GetReport)
            .WithSummary("Expedientes abiertos en un rango de fechas");
    }

    private static async Task<IResult> GetReport(
        string? from, string? to,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetOpenedFilesByPeriod(from, to), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
