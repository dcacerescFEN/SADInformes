using Application.Common.Mediator;
using Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValidities;
using Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValiditiesFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class TeacherValiditiesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/teacher-validities")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_TEACHER_VALIDITIES)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Años del informe de vigencias");
        group.MapGet("/", GetReport).WithSummary("Profesores confirmados por cátedra y año");
        group.MapGet("/download", Download).WithSummary("Vigencias de profesores en Excel");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetTeacherValiditiesFilters(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(string? year,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(new GetTeacherValidities(year), ct));
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? year,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetTeacherValidities(year), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            byte[] file;
            try { file = TeacherValiditiesExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ProfesoresVigencias.xlsx");
        }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Invalid(string message) =>
        Results.Problem(statusCode: 400, title: message);

    private static IResult Unavailable() =>
        Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
