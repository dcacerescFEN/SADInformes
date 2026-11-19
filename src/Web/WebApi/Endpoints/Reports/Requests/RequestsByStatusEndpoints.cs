using Application.Common.Mediator;
using Application.Common.Reporting;
using Application.Reports.Requests.RequestsByStatus.Commands.ConfirmRequestsPrinted;
using Application.Reports.Requests.RequestsByStatus.Commands.UpdateRequestValidationComment;
using Application.Reports.Requests.RequestsByStatus.Queries.GetRequestPrintDetail;
using Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatus;
using Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatusTypes;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Requests;

public sealed class RequestsByStatusEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/requests/by-status")
            .RequireAuthorization(Permission.REPORT_ACCESS, Permission.REPORT_VIEW_REQUESTS)
            .WithTags("Reports/Requests");
        group.MapGet("/types", GetTypes).WithSummary("Tipos de solicitudes para Otros Tipos");
        group.MapGet("/report", GetReport).WithSummary("Solicitudes por estado");
        group.MapPut("/comment", UpdateComment)
            .RequireAuthorization(Permission.REPORT_EDIT_REQUEST_VALIDATION)
            .WithSummary("Actualiza el comentario de validación");
        group.MapPost("/confirm-print", ConfirmPrint)
            .RequireAuthorization(Permission.REPORT_PRINT_REQUESTS)
            .WithSummary("Marca las solicitudes como impresas");
        group.MapGet("/print-detail", PrintDetail)
            .RequireAuthorization(Permission.REPORT_PRINT_REQUESTS)
            .WithSummary("Imprime el detalle de una solicitud");
    }

    private static async Task<IResult> GetTypes(
        string? career, string? status, string? typeFilter, IRequestDispatcher sender,
        HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetRequestsByStatusTypes(career, status, typeFilter), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> UpdateComment(
        UpdateRequestValidationCommentRequestDTO body, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            await sender.Send(new UpdateRequestValidationComment(body.RequestCode, body.Comment),
                cancellationToken);
            return Results.NoContent();
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (KeyNotFoundException error) { return Problem(404, error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> ConfirmPrint(
        ConfirmRequestsPrintedRequestDTO body, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new ConfirmRequestsPrinted(body.RequestCodes), cancellationToken));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
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

    private static async Task<IResult> PrintDetail(
        string? requestCode, IRequestDispatcher sender, HttpContext context,
        CancellationToken cancellationToken)
    {
        // La descarga contiene datos académicos y no debe quedar en caché.
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        try
        {
            var rendered = await sender.Send(
                new GetRequestPrintDetail(requestCode), cancellationToken);
            var fileNameCode = requestCode?.Replace("/", "", StringComparison.Ordinal).Trim() ?? "";
            return new ReportDownloadResult(rendered, $"Solicitud_{fileNameCode}");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return Unavailable(); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException or ReportRenderException;
    private static IResult Problem(int status, string title) => Results.Problem(statusCode: status, title: title);
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
