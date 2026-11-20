using Application.Common.Mediator;
using Application.Reports.Regulation.EgresadosMas3.Queries.GetGraduatesOverThreeYears;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Regulation;

public sealed class EgresadosMas3Endpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/regulation/graduates-over-3-years")
            .RequireAuthorization(Permission.REPORT_ACCESS, Permission.REPORT_VIEW_REGULATION)
            .WithTags("Reports/Regulation");
        group.MapGet("/report", GetReport)
            .WithSummary("Alumnos egresados hace más de tres años");
    }

    private static async Task<IResult> GetReport(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetGraduatesOverThreeYears(), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
