using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsFilters;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsLikert;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSample;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class ExitSurveyStatisticsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/exit-survey-statistics")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_STATISTICS)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", Filters).WithSummary("Filtros de estadísticas de encuesta de salida");
        group.MapGet("/sample", Sample).WithSummary("Muestra y avance de la encuesta de salida");
        group.MapGet("/likert", Likert).WithSummary("Matriz Likert Q1-Q7 por sección");
    }

    private static Task<IResult> Filters(HttpContext context, IRequestDispatcher sender, string? studentType, CancellationToken ct) =>
        Execute(context, async () => Results.Ok(await sender.Send(new GetExitSurveyStatisticsFilters(studentType), ct)));

    private static Task<IResult> Sample(HttpContext context, IRequestDispatcher sender,
        string? period, string? studentType, string? modality, CancellationToken ct) =>
        Execute(context, async () => Results.Ok(await sender.Send(
            new GetExitSurveyStatisticsSample(period, studentType, modality), ct)));

    private static Task<IResult> Likert(HttpContext context, IRequestDispatcher sender, string? section,
        string? period, string? studentType, string? modality, CancellationToken ct) =>
        Execute(context, async () => Results.Ok(await sender.Send(
            new GetExitSurveyStatisticsLikert(section, period, studentType, modality), ct)));

    private static async Task<IResult> Execute(HttpContext context, Func<Task<IResult>> operation)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return await operation(); }
        catch (ArgumentException error) { return Results.Problem(statusCode: 400, title: error.Message); }
        catch (Exception error) when (error is SqlException or InvalidOperationException or TimeoutException)
        {
            return Results.Problem(statusCode: 503, title: "El informe no está disponible.");
        }
    }
}
