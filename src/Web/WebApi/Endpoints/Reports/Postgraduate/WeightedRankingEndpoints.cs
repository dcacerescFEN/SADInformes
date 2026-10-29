using Application.Common.Mediator;
using Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRanking;
using Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRankingFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class WeightedRankingEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/weighted-ranking")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_WEIGHTED_RANKING)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters)
            .WithSummary("Filtros concretos del ranking de notas ponderadas");
        group.MapGet("/", GetReport)
            .WithSummary("Ranking por programa, modalidad, ingreso y año del cuadro de honor");
        group.MapGet("/download", Download)
            .WithSummary("Ranking de notas ponderadas en Excel, sin fotos");
    }

    private static async Task<IResult> GetFilters(string? studentType,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(new GetWeightedRankingFilters(studentType), ct));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(string? studentType, string? modality,
        string? cohort, string? year, IRequestDispatcher sender, HttpContext context,
        CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetWeightedRanking(studentType, modality, cohort, year), ct));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? studentType, string? modality,
        string? cohort, string? year, IRequestDispatcher sender, HttpContext context,
        CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(
                new GetWeightedRanking(studentType, modality, cohort, year), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            byte[] file;
            try { file = WeightedRankingExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Ranking_Programas_Graduados.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Invalid(string message) =>
        Results.Problem(statusCode: 400, title: message);

    private static IResult Unavailable() =>
        Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
