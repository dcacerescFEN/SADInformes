using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Commands.CancelStudentsWithoutPhotoEmail;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Commands.StartStudentsWithoutPhotoEmail;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailAvailability;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailPreview;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailStatus;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoPage;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.AcademicEnrollment;

public sealed class StudentsWithoutPhotoEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/students-without-photo")
            .RequireAuthorization(Permission.REPORT_ACCESS)
            .WithTags("Reports/AcademicEnrollment");
        group.MapGet("/report", GetReport).WithSummary("Alumnos sin fotografía registrada");
        group.MapGet("/export", Export).WithSummary("Alumnos sin fotografía en Excel");
        group.MapGet("/email/availability", GetEmailAvailability)
            .WithSummary("Disponibilidad del envío de correo");
        group.MapGet("/email/preview", GetEmailPreview)
            .WithSummary("Vista previa del correo de alumnos sin foto");
        group.MapPost("/email", StartEmail).WithSummary("Inicia el envío asíncrono de correos");
        group.MapGet("/email/{jobId:guid}", GetEmailStatus).WithSummary("Estado del envío de correos");
        group.MapPost("/email/{jobId:guid}/cancel", CancelEmail)
            .WithSummary("Cancela un envío de correos en curso");
    }

    private static async Task<IResult> GetReport(
        int page, int pageSize, string? search,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetStudentsWithoutPhotoPage(page, pageSize, search), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Export(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetStudentsWithoutPhoto(), cancellationToken);
            if (report.Data.Rows.Length == 0) return Invalid("Sin datos.");
            byte[] file;
            try { file = StudentsWithoutPhotoExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SinFotos.xlsx");
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetEmailAvailability(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetStudentsWithoutPhotoEmailAvailability(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetEmailPreview(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetStudentsWithoutPhotoEmailPreview(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> StartEmail(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var result = await sender.Send(new StartStudentsWithoutPhotoEmail(), cancellationToken);
            return Results.Accepted(
                $"/api/reports/academic-enrollment/students-without-photo/email/{result.Data.JobId}", result);
        }
        catch (EmailDispatchConflictException error) { return Problem(409, error.Message); }
        catch (InvalidOperationException error) { return Problem(503, error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetEmailStatus(
        Guid jobId, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetStudentsWithoutPhotoEmailStatus(jobId), cancellationToken)); }
        catch (KeyNotFoundException) { return Problem(404, "El envío de correo no existe."); }
    }

    private static async Task<IResult> CancelEmail(
        Guid jobId, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new CancelStudentsWithoutPhotoEmail(jobId), cancellationToken)); }
        catch (KeyNotFoundException) { return Problem(404, "El envío de correo no existe."); }
        catch (EmailDispatchConflictException error) { return Problem(409, error.Message); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Problem(int status, string title) => Results.Problem(statusCode: status, title: title);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
