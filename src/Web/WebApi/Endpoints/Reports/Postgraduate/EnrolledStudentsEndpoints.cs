using Application.Common.Mediator;
using Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudents;
using Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudentsFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class EnrolledStudentsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/enrolled-students")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_ENROLLED_STUDENTS)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters)
            .WithSummary("Modalidades fijas y años de alumnos matriculados MBA");
        group.MapGet("/download", Download)
            .WithSummary("Exportación de alumnos matriculados MBA en Excel");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(new GetEnrolledStudentsFilters(), ct));
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? modality, string? year,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetEnrolledStudents(modality, year), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            byte[] file;
            try { file = EnrolledStudentsExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "AlumnosMatriculados.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Invalid(string message) =>
        Results.Problem(statusCode: 400, title: message);

    private static IResult Unavailable() =>
        Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
