using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;
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

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
