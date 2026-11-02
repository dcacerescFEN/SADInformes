using Application.Common.Mediator;
using Application.Reports.Postgraduate.AlumniSurveyStatistics.Queries.GetAlumniSurvey;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class AlumniSurveyStatisticsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGroup("/api/reports/postgraduate/alumni-survey-statistics")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_ALUMNI_SURVEY_STATISTICS)
            .WithTags("Reports/Postgraduate")
            .MapGet("/download", Download).WithSummary("Estadísticas encuesta Alumni en Excel");

    private static async Task<IResult> Download(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetAlumniSurvey(), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(AlumniSurveyExcel.Create(report.Data.Rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "EncuestaAlumniPostgrado.xlsx");
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
