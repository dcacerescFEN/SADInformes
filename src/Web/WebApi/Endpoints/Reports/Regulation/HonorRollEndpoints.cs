using Application.Common.Mediator;
using Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRoll;
using Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRollFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Regulation;

public sealed class HonorRollEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/others/honor-roll")
            .RequireAuthorization(
                Permission.REPORT_ACCESS,
                Permission.REPORT_VIEW_OTHER,
                Permission.REPORT_VIEW_HONOR_ROLL)
            .WithTags("Reports/Regulation");
        group.MapGet("/filters", GetFilters)
            .WithSummary("Carreras y años del Cuadro de Honor");
        group.MapGet("/report", GetReport)
            .WithSummary("Cuadro de Honor por carrera, año de ingreso y año del cuadro");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetHonorRollFilters(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(
        string? career, string? entryYear, string? honorYear,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetHonorRoll(career, entryYear, honorYear), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
