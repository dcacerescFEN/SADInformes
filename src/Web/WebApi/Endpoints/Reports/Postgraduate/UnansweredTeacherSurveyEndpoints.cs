using Application.Common.Mediator;
using Application.Reports.Postgraduate.UnansweredTeacherSurvey.Queries.GetUnansweredTeacherSurvey;
using Application.Reports.Postgraduate.UnansweredTeacherSurvey.Queries.GetUnansweredTeacherSurveyFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

/// <summary>Informe Alumnos sin contestar encuesta docente. Requiere su permiso específico.</summary>
public sealed class UnansweredTeacherSurveyEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/unanswered-teacher-survey")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY)
            .WithTags("Reports/Postgraduate");

        group.MapGet("/filters", GetFilters).WithSummary("Catálogos del informe Alumnos sin contestar encuesta docente");
        group.MapGet("/", GetReport).WithSummary("Alumnos sin contestar encuesta docente filtrada");
    }

    private static async Task<IResult> GetFilters(
        string? tipoAlumno, IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(new GetUnansweredTeacherSurveyFilters(tipoAlumno), cancellationToken));
        }
        catch (ArgumentException error)
        {
            return Results.Problem(statusCode: 400, title: error.Message);
        }
        catch (Exception error) when (IsUnavailable(error))
        {
            return Unavailable();
        }
    }

    private static async Task<IResult> GetReport(
        string? periodo, string? tipoAlumno, string? modalidad,
        IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(
                new GetUnansweredTeacherSurvey(periodo ?? string.Empty, tipoAlumno ?? string.Empty, modalidad ?? string.Empty),
                cancellationToken));
        }
        catch (ArgumentException error)
        {
            return Results.Problem(statusCode: 400, title: error.Message);
        }
        catch (Exception error) when (IsUnavailable(error))
        {
            return Unavailable();
        }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Unavailable() =>
        Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
