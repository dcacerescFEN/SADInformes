using Application.Common.Mediator;
using Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatus;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Requests;

public sealed class RequestsByStatusEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/requests/by-status")
            .RequireAuthorization(Permission.REPORT_ACCESS, Permission.REPORT_VIEW_REQUESTS)
            .WithTags("Reports/Requests");
        group.MapGet("/report", GetReport).WithSummary("Solicitudes por estado");
    }

    private static async Task<IResult> GetReport(
        string? career, string? status, string? type, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetRequestsByStatus(career, status, type), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
