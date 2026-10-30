using Application.Common.Mediator;
using Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalData;
using Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalDataFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class PersonalDataEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/personal-data")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_PERSONAL_DATA)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Catálogos de datos personales");
        group.MapGet("/download", Download).WithSummary("Datos personales en Excel");
    }

    private static async Task<IResult> GetFilters(string? studentType,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetPersonalDataFilters(studentType), ct)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? studentType, string? modality, string? year,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetPersonalData(studentType, modality, year), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(PersonalDataExcel.Create(report.Data.Rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Datos_Personales_Postgrado.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;
    private static IResult Invalid(string message) => Results.Problem(statusCode: 400, title: message);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
