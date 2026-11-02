using Application.Common.Mediator;
using Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsFilters;
using Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsSummary;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class IssStatisticsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/iss-statistics")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_ISS_STATISTICS)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Catálogos de estadísticas ISS");
        group.MapGet("/summary", GetSummary).WithSummary("Resumen de estadísticas ISS");
    }

    private static async Task<IResult> GetFilters(string? period,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetIssStatisticsFilters(period), ct)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetSummary(string? period, string? courseSection,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetIssStatisticsSummary(period, courseSection), ct)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
