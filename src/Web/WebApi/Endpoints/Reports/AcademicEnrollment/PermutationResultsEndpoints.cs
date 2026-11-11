using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationFilters;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationProcesses;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationResults;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.AcademicEnrollment;

public sealed class PermutationResultsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/permutation-results-by-student")
            .RequireAuthorization(Permission.REPORT_ACCESS)
            .WithTags("Reports/AcademicEnrollment");
        group.MapGet("/filters", GetFilters).WithSummary("Períodos activos de toma de ramos");
        group.MapGet("/processes", GetProcesses).WithSummary("Rangos del proceso de permuta");
        group.MapGet("/report", GetReport).WithSummary("Resultados de permuta por alumno");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetPermutationFilters(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetProcesses(
        string? period, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetPermutationProcesses(period), cancellationToken)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(
        string? period, string? from, string? to,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetPermutationResults(period, from, to), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
