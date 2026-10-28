using Application.Common.Interfaces;
using Application.Reports.Postgraduate.MassEnrollment;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class MassEnrollmentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/mass-enrollment")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_MASS_ENROLLMENT)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", Filters).WithSummary("Filtros de inscripción masiva");
        group.MapGet("/", Candidates).WithSummary("Alumnos disponibles para inscripción masiva");
        group.MapPost("/", Enroll)
            .RequireAuthorization(Permission.REPORT_MANAGE_POSTGRADUATE_MASS_ENROLLMENT)
            .WithSummary("Inscribir el lote confirmado de alumnos");
    }

    private static Task<IResult> Filters(HttpContext context, MassEnrollmentService service,
        IApiResponseService responses, string? studentType, string? modality, string? cohort,
        string? period, string? course, CancellationToken ct) =>
        Execute(async () => Results.Ok(responses.Success(await service.FiltersAsync(
            Selection(studentType, modality, cohort, period, course), User(context), ct))));

    private static Task<IResult> Candidates(HttpContext context, MassEnrollmentService service,
        IApiResponseService responses, string? studentType, string? modality, string? cohort,
        string? period, string? course, CancellationToken ct) =>
        Execute(async () => Results.Ok(responses.Success(await service.CandidatesAsync(
            Selection(studentType, modality, cohort, period, course), User(context), ct))));

    private static Task<IResult> Enroll(HttpContext context, MassEnrollmentService service,
        IApiResponseService responses, EnrollmentRequest request, CancellationToken ct) =>
        Execute(async () => Results.Ok(responses.Success(await service.EnrollAsync(request,
            User(context), context.Connection.RemoteIpAddress?.ToString() ?? "desconocida", ct))));

    private static EnrollmentSelection Selection(string? type, string? modality, string? cohort, string? period, string? course) =>
        new(type ?? "", modality ?? "", cohort ?? "", period ?? "", course ?? "");

    private static string User(HttpContext context) => context.User.Identity?.Name ?? "";

    private static async Task<IResult> Execute(Func<Task<IResult>> operation)
    {
        try { return await operation(); }
        catch (ArgumentException error) { return Results.Problem(statusCode: 400, title: error.Message); }
        catch (EnrollmentConflictException error) { return Results.Problem(statusCode: 409, title: error.Message); }
        catch (Exception error) when (error is SqlException or InvalidOperationException or TimeoutException)
        {
            return Results.Problem(statusCode: 503,
                title: "No se pudo confirmar la operación. Actualice el listado antes de reintentar.");
        }
    }
}
