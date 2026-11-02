using Application.Common.Mediator;
using Application.Reports.Postgraduate.CourseActivities.Queries.GetActivities;
using Application.Reports.Postgraduate.CourseActivities.Queries.GetActivityFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class CourseActivitiesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/course-activities")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_COURSE_ACTIVITIES)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Años de actividades de cursos");
        group.MapGet("/report", GetReport).WithSummary("Actividades de cursos de Postgrado");
        group.MapGet("/download", Download).WithSummary("Actividades de cursos en Excel");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetActivityFilters(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(string? year,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetActivities(year), ct)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? year,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetActivities(year), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(CourseActivitiesExcel.Create(report.Data.Rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reporte Actividades Cursos Postgrado - {report.Data.Year}.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
