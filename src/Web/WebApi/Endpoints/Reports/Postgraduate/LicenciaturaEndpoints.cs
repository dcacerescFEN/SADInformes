using Application.Common.Mediator;
using Application.Reports.Postgraduate.Licenciatura.Queries.GetLicenciatura;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class LicenciaturaEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/licenciatura-pregrado")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_LICENCIATURA)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/report", GetReport).WithSummary("Alumnos próximos a licenciatura");
        group.MapGet("/download", Download).WithSummary("Próximas licenciaturas en Excel");
    }

    private static async Task<IResult> GetReport(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetLicenciatura(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetLicenciatura(), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(LicenciaturaExcel.Create(report.Data.Rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ProximasLicenciatura.xlsx");
        }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
