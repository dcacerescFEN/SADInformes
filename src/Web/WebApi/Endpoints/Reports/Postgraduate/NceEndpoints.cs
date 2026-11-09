using Application.Common.Mediator;
using Application.Reports.Postgraduate.Nce.Queries.GetNceReport;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class NceEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/nce")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/report", Report).WithSummary("Informe de notificaciones NCE");
        group.MapGet("/download", Download).WithSummary("Informe NCE en Excel");
        group.MapGet("/download/pdf", DownloadPdf).WithSummary("Informe NCE en PDF");
    }

    private static async Task<IResult> Report(HttpContext context, IRequestDispatcher sender, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetNceReport(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(HttpContext context, IRequestDispatcher sender, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetNceReport(), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(NceReportExcel.Create(report.Data.Rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Informe Notificaciones Causal de Eliminación - {DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> DownloadPdf(HttpContext context, IRequestDispatcher sender, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetNceReport(), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(NceReportPdf.Create(report.Data.Rows), "application/pdf",
                $"Informe Notificaciones Causal de Eliminación - {DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
