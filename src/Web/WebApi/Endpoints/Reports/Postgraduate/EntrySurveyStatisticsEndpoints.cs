using Application.Common.Mediator;
using Application.Reports.Postgraduate.EntrySurveyStatistics.Queries.GetEntrySurveyStatistics;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class EntrySurveyStatisticsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/entry-survey-statistics")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_ENTRY_SURVEY_STATISTICS)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/", GetReport).WithSummary("Respuestas de la encuesta de entrada");
        group.MapGet("/download", Download).WithSummary("Encuesta de entrada en Excel");
    }

    private static async Task<IResult> GetReport(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetEntrySurveyStatistics(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetEntrySurveyStatistics(), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            byte[] file;
            try { file = EntrySurveyExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "EncuestaEntradaPostgrado.xlsx");
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Invalid(string message) =>
        Results.Problem(statusCode: 400, title: message);

    private static IResult Unavailable() =>
        Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
