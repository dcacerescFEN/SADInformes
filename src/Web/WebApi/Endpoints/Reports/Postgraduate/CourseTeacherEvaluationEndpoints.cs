using Application.Common.Mediator;
using Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluation;
using Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluationFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class CourseTeacherEvaluationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/course-teacher-evaluation")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_COURSE_TEACHER_EVALUATION)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Años y tipos de evaluación");
        group.MapGet("/download", Download).WithSummary("Evaluación docente/curso en Excel");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetCourseTeacherEvaluationFilters(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? year, string? type,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetCourseTeacherEvaluation(year, type), ct);
            if (report.Data.Total == 0) return Invalid("Sin Datos");
            var file = report.Data.Type == "EX"
                ? CourseTeacherEvaluationExcel.Executive(report.Data.Executive)
                : CourseTeacherEvaluationExcel.FullTime(report.Data.FullTime);
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Evaluacion_Docente_Curso.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
