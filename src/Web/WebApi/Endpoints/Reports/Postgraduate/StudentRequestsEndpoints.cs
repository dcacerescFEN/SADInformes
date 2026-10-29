using Application.Common.Mediator;
using Application.Common.Reporting;
using Application.Reports.Postgraduate.StudentRequests.Queries.GetStudentRequests;
using Application.Reports.Postgraduate.StudentRequests.Queries.GetStudentRequestsFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class StudentRequestsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/student-requests")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_STUDENT_REQUESTS)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Filtros de Solicitudes alumnos Postgrado");
        group.MapGet("/", GetReport).WithSummary("Descarga Excel SSRS de Solicitudes alumnos Postgrado");
    }

    private static async Task<IResult> GetFilters(string? studentType, IRequestDispatcher sender, CancellationToken ct)
    {
        try { return Results.Ok(await sender.Send(new GetStudentRequestsFilters(studentType), ct)); }
        catch (ArgumentException error) { return Results.Problem(statusCode: 400, title: error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(string? studentType, string? modality, string? cohort,
        string? state, string? format, IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        try
        {
            var rendered = await sender.Send(new GetStudentRequests(studentType, modality, cohort, state, format), ct);
            // Authenticated academic reports must not be cached by browsers or intermediaries.
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return new ReportDownloadResult(rendered, "SolicitudesAlumnoPostgrado");
        }
        catch (ArgumentException error) { return Results.Problem(statusCode: 400, title: error.Message); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Unavailable(); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException or ReportRenderException;
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
