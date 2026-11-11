using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationFilters;
using Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationProcesses;
using Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationResultsByCourse;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.AcademicEnrollment;

public sealed class PostulationResultsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/postulation-results-by-course")
            .RequireAuthorization(Permission.REPORT_ACCESS)
            .WithTags("Reports/AcademicEnrollment");
        group.MapGet("/filters", GetFilters).WithSummary("Períodos activos de toma de ramos");
        group.MapGet("/processes", GetProcesses).WithSummary("Rangos del proceso de postulación");
        group.MapGet("/report", GetReport).WithSummary("Resultados del procesamiento por curso");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetPostulationFilters(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetProcesses(
        string? origin, string? period,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetPostulationProcesses(origin, period), cancellationToken)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(
        string? origin, string? period, string? from, string? to,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetPostulationResultsByCourse(period, origin, from, to), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
