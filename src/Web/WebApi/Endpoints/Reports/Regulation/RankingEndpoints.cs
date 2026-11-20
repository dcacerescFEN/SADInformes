using Application.Common.Mediator;
using Application.Reports.Regulation.Ranking.Queries.GetRanking;
using Application.Reports.Regulation.Ranking.Queries.GetRankingFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Regulation;

public sealed class RankingEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/regulation/ranking")
            .RequireAuthorization(Permission.REPORT_ACCESS, Permission.REPORT_VIEW_REGULATION)
            .WithTags("Reports/Regulation");
        group.MapGet("/filters", GetFilters)
            .WithSummary("Períodos académicos del ranking de egreso");
        group.MapGet("/report", GetReport)
            .WithSummary("Ranking de licenciatura y egreso por grupo");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetRankingFilters(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(
        string? group, string? period, string? selection, string? sortBy, string? direction,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetRanking(group, period, selection, sortBy, direction), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
