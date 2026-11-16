using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.StartTeacherAvailabilityEmail;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.CancelTeacherAvailabilityEmail;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.DeleteTeacherAvailability;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.SaveTeacherAvailabilityEditor;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityEditor;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.UpdateTeacherAvailabilitySettings;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilitySettings;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityEmailStatus;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.AcademicEnrollment;

public sealed class TeacherAvailabilityEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/teacher-availability")
            .RequireAuthorization(Permission.REPORT_VIEW_AVAILABILITY)
            .WithTags("Reports/AcademicEnrollment");
        group.MapGet("/report", GetReport).WithSummary("Disponibilidad horaria de profesores");
        group.MapGet("/observations", GetObservations).WithSummary("Observaciones de disponibilidad");
        group.MapGet("/settings", GetSettings).RequireAuthorization(Permission.REPORT_EDIT_AVAILABILITY)
            .WithSummary("Configuración del período de disponibilidad");
        group.MapPut("/settings", UpdateSettings).RequireAuthorization(Permission.REPORT_EDIT_AVAILABILITY)
            .WithSummary("Actualiza la configuración del período de disponibilidad");
        group.MapGet("/editor", GetEditor).RequireAuthorization(Permission.REPORT_EDIT_AVAILABILITY)
            .WithSummary("Datos para editar la disponibilidad de un profesor");
        group.MapPut("/editor", SaveEditor).RequireAuthorization(Permission.REPORT_EDIT_AVAILABILITY)
            .WithSummary("Guarda la edición de disponibilidad de un profesor");
        group.MapDelete("/report/{teacherCode}", DeleteReport)
            .RequireAuthorization(Permission.REPORT_EDIT_AVAILABILITY)
            .WithSummary("Elimina la disponibilidad de un profesor");
        group.MapPost("/email", StartEmail).RequireAuthorization(Permission.REPORT_SEND_AVAILABILITY_EMAIL)
            .WithSummary("Inicia el envío asíncrono de correos de disponibilidad");
        group.MapGet("/email/{jobId:guid}", GetEmailStatus).RequireAuthorization(Permission.REPORT_SEND_AVAILABILITY_EMAIL)
            .WithSummary("Estado del envío de correos de disponibilidad");
        group.MapPost("/email/{jobId:guid}/cancel", CancelEmail).RequireAuthorization(Permission.REPORT_SEND_AVAILABILITY_EMAIL)
            .WithSummary("Cancela un envío de correos de disponibilidad");
    }

    private static async Task<IResult> GetReport(
        string? period, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetTeacherAvailability(period, context.User.Identity?.Name), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetObservations(
        string? period, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetTeacherAvailabilityObservations(period), cancellationToken)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetEditor(
        string? period, string? teacherCode, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetTeacherAvailabilityEditor(period, teacherCode), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> SaveEditor(
        SaveTeacherAvailabilityEditorRequestDTO body, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            await sender.Send(new SaveTeacherAvailabilityEditor(
                body.Period, body.TeacherCode, body.Courses, body.Modules, body.NewModules),
                cancellationToken);
            return Results.NoContent();
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> DeleteReport(
        string teacherCode, string? period, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            await sender.Send(new DeleteTeacherAvailability(period, teacherCode), cancellationToken);
            return Results.NoContent();
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetSettings(
        string? period, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetTeacherAvailabilitySettings(period, context.User.Identity?.Name), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> UpdateSettings(
        UpdateTeacherAvailabilitySettingsRequestDTO body, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            await sender.Send(new UpdateTeacherAvailabilitySettings(
                body.Period, body.StartDate, body.EndDate, body.StartTime, body.EndTime,
                body.PendingEmailText, body.LaggardEmailText, body.ProcessPeriod), cancellationToken);
            return Results.NoContent();
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> StartEmail(
        StartTeacherAvailabilityEmailRequestDTO body, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var result = await sender.Send(new StartTeacherAvailabilityEmail(
                body.Period, context.User.Identity?.Name, body.Mode, body.TeacherCode), cancellationToken);
            return Results.Accepted(
                $"/api/reports/academic-enrollment/teacher-availability/email/{result.Data.JobId}", result);
        }
        catch (TeacherAvailabilityEmailConflictException error) { return Problem(409, error.Message); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (InvalidOperationException error) { return Problem(503, error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetEmailStatus(
        Guid jobId, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetTeacherAvailabilityEmailStatus(jobId), cancellationToken)); }
        catch (KeyNotFoundException) { return Problem(404, "El envío de correo no existe."); }
    }

    private static async Task<IResult> CancelEmail(
        Guid jobId, IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new CancelTeacherAvailabilityEmail(jobId), cancellationToken)); }
        catch (KeyNotFoundException) { return Problem(404, "El envío de correo no existe."); }
        catch (TeacherAvailabilityEmailConflictException error) { return Problem(409, error.Message); }
    }

    private static IResult Problem(int status, string title) => Results.Problem(statusCode: status, title: title);

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
