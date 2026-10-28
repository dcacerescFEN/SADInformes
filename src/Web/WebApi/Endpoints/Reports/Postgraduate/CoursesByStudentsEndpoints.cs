using Application.Common.Mediator;
using Application.Reports.Postgraduate.CoursesByStudents.Queries;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

/// <summary>Informe Cursos vs. alumnos. Requiere su permiso específico.</summary>
public sealed class CoursesByStudentsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/courses-by-students")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_COURSES_BY_STUDENT)
            .WithTags("Reports/Postgraduate");

        group.MapGet("/filters", GetFilters).WithSummary("Catálogos del informe Cursos vs. alumnos");
        group.MapGet("/", GetReport).WithSummary("Cursos vs. alumnos filtrada");
    }

    private static async Task<IResult> GetFilters(
        string? tipoAlumno, IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(new GetCoursesByStudentsFilters(tipoAlumno), cancellationToken));
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
                new GetCoursesByStudents(periodo ?? string.Empty, tipoAlumno ?? string.Empty, modalidad ?? string.Empty),
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
