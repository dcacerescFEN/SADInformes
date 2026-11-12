using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationFilters;
using Application.Reports.AcademicEnrollment.RoomAssignmentCourses.Queries.GetRoomAssignmentCourses;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.AcademicEnrollment;

public sealed class RoomAssignmentCoursesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/courses-for-room-assignment")
            .RequireAuthorization(Permission.REPORT_ACCESS)
            .WithTags("Reports/AcademicEnrollment");
        group.MapGet("/filters", GetFilters).WithSummary("Períodos activos de toma de ramos");
        group.MapGet("/report", GetReport).WithSummary("Cursos para asignación de salas");
        group.MapGet("/export", Export).WithSummary("Cursos para asignación de salas en Excel");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetPermutationFilters(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(
        string? period, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetRoomAssignmentCourses(period), cancellationToken)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Export(
        string? period, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetRoomAssignmentCourses(period), cancellationToken);
            if (report.Data.Rows.Length == 0) return Invalid("Sin datos.");
            byte[] file;
            try { file = RoomAssignmentCoursesExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CursosAsignacionSalas{report.Data.Period}.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
