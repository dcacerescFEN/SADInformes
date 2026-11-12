using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingSchedule;
using Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingScheduleFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.AcademicEnrollment;

public sealed class EnrollmentTeachingScheduleEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/enrollment-teaching-schedule")
            .RequireAuthorization(Permission.REPORT_ACCESS)
            .WithTags("Reports/AcademicEnrollment");
        group.MapGet("/filters", GetFilters).WithSummary("Períodos y áreas de programación docente");
        group.MapGet("/report", GetReport).WithSummary("Programación docente resumida");
        group.MapGet("/export", Export).WithSummary("Programación docente en Excel");
    }

    private static async Task<IResult> GetFilters(
        string? school, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetEnrollmentTeachingScheduleFilters(school), cancellationToken)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(
        string? school, string? period, string? area,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetEnrollmentTeachingSchedule(school, period, area), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Export(
        string? school, string? period, string? area,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(
                new GetEnrollmentTeachingSchedule(school, period, area), cancellationToken);
            if (report.Data.Rows.Length == 0) return Invalid("Sin datos.");
            byte[] file;
            try { file = EnrollmentTeachingScheduleExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ProgramacionDocente.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
