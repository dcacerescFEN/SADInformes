using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Commands.CreateExitSurveyEnrollment;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Common;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollment;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollmentFilters;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class ExitSurveyEnrollmentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/exit-survey-enrollment")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_ENROLLMENT)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", Filters).WithSummary("Filtros de inscripción a encuesta de salida");
        group.MapGet("/", Listing).WithSummary("Alumnos de encuesta de salida por tipo, modalidad y año");
        group.MapPost("/", Enroll)
            .RequireAuthorization(Permission.REPORT_MANAGE_POSTGRADUATE_EXIT_SURVEY_ENROLLMENT)
            .WithSummary("Inscribe un lote confirmado en la encuesta de salida");
    }

    private static Task<IResult> Filters(HttpContext context, IRequestDispatcher sender, string? studentType, CancellationToken ct) =>
        Execute(context, false, async () => Results.Ok(await sender.Send(new GetExitSurveyEnrollmentFilters(studentType), ct)));

    private static Task<IResult> Listing(HttpContext context, IRequestDispatcher sender, IAuthorizationService authorization,
        string? studentType, string? modality, string? year, CancellationToken ct) =>
        Execute(context, false, async () =>
        {
            var canEnroll = (await authorization.AuthorizeAsync(context.User,
                Permission.REPORT_MANAGE_POSTGRADUATE_EXIT_SURVEY_ENROLLMENT)).Succeeded;
            return Results.Ok(await sender.Send(new GetExitSurveyEnrollment(studentType, modality, year, canEnroll), ct));
        });

    private static Task<IResult> Enroll(HttpContext context, IRequestDispatcher sender, CreateExitSurveyEnrollmentRequestDTO request, CancellationToken ct) =>
        Execute(context, true, async () => Results.Ok(await sender.Send(new CreateExitSurveyEnrollment(request), ct)));

    private static async Task<IResult> Execute(HttpContext context, bool write, Func<Task<IResult>> operation)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return await operation(); }
        catch (ArgumentException error) { return Results.Problem(statusCode: 400, title: error.Message); }
        catch (ExitSurveyConflictException error) { return Results.Problem(statusCode: 409, title: error.Message); }
        catch (Exception error) when (error is SqlException or InvalidOperationException or TimeoutException)
        {
            return Results.Problem(statusCode: 503, title: write
                ? "No se pudo confirmar la inscripción. Actualice el listado antes de reintentar."
                : "El informe no está disponible.");
        }
    }
}
