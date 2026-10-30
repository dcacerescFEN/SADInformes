using Application.Common.Mediator;
using Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefenses;
using Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefensesFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class ThesisDefensesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/thesis-defenses")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_THESIS_DEFENSES)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters)
            .WithSummary("Años, tipos de alumno y modalidades de defensas de tesis");
        group.MapGet("/", GetReport)
            .WithSummary("Defensas de tesis: Coordinación y Tesorería");
        group.MapGet("/download/coordination", DownloadCoordination)
            .WithSummary("Reporte de comisiones formato Coordinación");
        group.MapGet("/download/treasury", DownloadTreasury)
            .WithSummary("Reporte de comisiones formato Tesorería");
    }

    private static async Task<IResult> GetFilters(string? studentType,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(new GetThesisDefensesFilters(studentType), ct));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(string? defenseYear, string? studentType,
        string? modality, string? entryYear, IRequestDispatcher sender, HttpContext context,
        CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(
                new GetThesisDefenses(defenseYear, studentType, modality, entryYear), ct));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static Task<IResult> DownloadCoordination(string? defenseYear, string? studentType,
        string? modality, string? entryYear, IRequestDispatcher sender, HttpContext context,
        CancellationToken ct) => Download(true, defenseYear, studentType, modality, entryYear,
            sender, context, ct);

    private static Task<IResult> DownloadTreasury(string? defenseYear, string? studentType,
        string? modality, string? entryYear, IRequestDispatcher sender, HttpContext context,
        CancellationToken ct) => Download(false, defenseYear, studentType, modality, entryYear,
            sender, context, ct);

    private static async Task<IResult> Download(bool coordination, string? defenseYear,
        string? studentType, string? modality, string? entryYear, IRequestDispatcher sender,
        HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(
                new GetThesisDefenses(defenseYear, studentType, modality, entryYear), ct);
            var rows = coordination ? report.Data.CoordinationTotal : report.Data.TreasuryTotal;
            if (rows == 0) return Invalid("Sin Datos");
            byte[] file;
            try
            {
                file = coordination
                    ? ThesisDefensesExcel.Coordination(report.Data.Coordination)
                    : ThesisDefensesExcel.Treasury(report.Data.Treasury);
            }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            var name = coordination ? "ReporteComisionesCoordinacion" : "ReporteComisionesTesoreria";
            var filters = string.Join("-", new[] { defenseYear, studentType, modality, entryYear }
                .Select(value => Safe(string.IsNullOrWhiteSpace(value) ? "Todos" : value)));
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{name}-{filters}.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static string Safe(string value) =>
        string.Concat(value.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Invalid(string message) =>
        Results.Problem(statusCode: 400, title: message);

    private static IResult Unavailable() =>
        Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
