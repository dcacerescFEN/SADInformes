using Application.Common.Mediator;
using Application.Reports.Postgraduate.TeacherCourses.Queries.GetTeacherCourses;
using Application.Reports.Postgraduate.TeacherCourses.Queries.GetTeacherCoursesFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

/// <summary>Informe Cursos Profesores Postgrado. Requiere su permiso específico.</summary>
public sealed class TeacherCoursesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/teacher-courses")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES)
            .WithTags("Reports/Postgraduate");

        group.MapGet("/filters", GetFilters).WithSummary("Catálogos del informe Cursos Profesores Postgrado");
        group.MapGet("/", GetReport).WithSummary("Cursos Profesores Postgrado filtrado");
    }

    private static async Task<IResult> GetFilters(IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(new GetTeacherCoursesFilters(), cancellationToken));
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
        string? periodo, string? departamento, IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(
                new GetTeacherCourses(periodo ?? string.Empty, departamento ?? string.Empty), cancellationToken));
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
