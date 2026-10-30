using Application.Common.Mediator;
using Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCourses;
using Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCoursesFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;
using WebApi.Reporting;

namespace WebApi.Endpoints.Reports.Postgraduate;

public sealed class AreaCoursesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/area-courses")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_AREA_COURSES)
            .WithTags("Reports/Postgraduate");
        group.MapGet("/filters", GetFilters).WithSummary("Años del reporte de áreas y cátedras");
        group.MapGet("/", GetReport).WithSummary("Cátedras y áreas para dos años");
        group.MapGet("/download", Download).WithSummary("Reporte de áreas y cátedras en Excel");
    }

    private static async Task<IResult> GetFilters(
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetAreaCoursesFilters(), ct)); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> GetReport(string? year1, string? year2,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await sender.Send(new GetAreaCourses(year1, year2), ct)); }
        catch (ArgumentException error) { return Invalid(error.Message); }
        catch (Exception error) when (IsUnavailable(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Download(string? year1, string? year2,
        IRequestDispatcher sender, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var report = await sender.Send(new GetAreaCourses(year1, year2), ct);
            if (report.Data.Rows.Length == 0) return Invalid("Sin Datos");
            byte[] file;
            try { file = AreaCoursesExcel.Create(report.Data.Rows); }
            catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
            { return Unavailable(); }
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "AlumnosPregradoPostgrado.xlsx");
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
