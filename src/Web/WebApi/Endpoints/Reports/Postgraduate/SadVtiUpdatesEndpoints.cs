using Application.Common.Mediator;
using Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdates;
using Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdateFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class SadVtiUpdatesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/sad-vti-updates")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Estados de actualización SAD–VTI");
        group.MapGet("/download", Download).WithSummary("Descarga Excel de auditoría SAD–VTI por estado y fechas inclusivas");
    }

    private static async Task<IResult> GetFilters(IRequestDispatcher sender, CancellationToken ct)
    {
        try { return Results.Ok(await sender.Send(new GetSadVtiUpdateFilters(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? state, string? from, string? to,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        try
        {
            var report = await sender.Send(new GetSadVtiUpdates(state, from, to), ct);
            if (report.Data.Rows.Length == 0)
                return Results.Problem(statusCode: 400, title: "No hay registros para exportar.");
            byte[] file;
            try { file = SadVtiExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException) { return Unavailable(); }
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "AudiPostgradoAlumnosGuia.xlsx");
        }
        catch (ArgumentException error) { return Results.Problem(statusCode: 400, title: error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) => error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
