using Application.Common.Mediator;
using Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingFilters;
using Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingRequests;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Requests;

public sealed class AutomaticProcessingRequestsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/requests/automatic-processing")
            .RequireAuthorization(Permission.REPORT_ACCESS, Permission.REPORT_VIEW_REQUESTS)
            .WithTags("Reports/Requests");
        group.MapGet("/filters", GetFilters).WithSummary("Filtros de solicitudes procesadas automáticamente");
        group.MapGet("/report", GetReport).WithSummary("Solicitudes procesadas automáticamente");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetAutomaticProcessingFilters(), cancellationToken)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(
        string? period, string? career, string? type, string? status,
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetAutomaticProcessingRequests(period, career, type, status), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
