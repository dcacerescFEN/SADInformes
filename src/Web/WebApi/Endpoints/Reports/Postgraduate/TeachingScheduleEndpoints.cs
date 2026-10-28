using Application.Common.Mediator;
using Application.Reports.Postgraduate.TeachingSchedule.Queries.GetTeachingSchedule;
using Application.Reports.Postgraduate.TeachingSchedule.Queries.GetTeachingScheduleFilters;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.Postgraduate;

/// <summary>Informe Programación Docente Postgrado X Programa y Modalidad. Requiere su permiso específico.</summary>
public sealed class TeachingScheduleEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/postgraduate/teaching-schedule")
            .RequireAuthorization(Permission.REPORT_VIEW_POSTGRADUATE_TEACHING_SCHEDULE)
            .WithTags("Reports/Postgraduate");

        group.MapGet("/filters", GetFilters).WithSummary("Catálogos del informe Programación Docente Postgrado X Programa y Modalidad");
        group.MapGet("/", GetReport).WithSummary("Programación Docente Postgrado X Programa y Modalidad filtrada");
    }

    private static async Task<IResult> GetFilters(
        string? tipoPeriodo, string? tipoAlumno, IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(new GetTeachingScheduleFilters(tipoPeriodo ?? "PERÍODO", tipoAlumno), cancellationToken));
        }
        catch (ArgumentException error)
        {
            return Results.Problem(statusCode: 400, title: error.Message);
        }
        catch (Exception error) when (IsUnavailable(error))
        {
            return Unavailable();
        }
    }

    private static async Task<IResult> GetReport(
        string? tipoPeriodo, string? periodo, string? tipoAlumno, string? modalidad, string? promocion,
        IRequestDispatcher sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(
                new GetTeachingSchedule(tipoPeriodo ?? string.Empty, periodo ?? string.Empty, tipoAlumno ?? string.Empty, modalidad, promocion),
                cancellationToken));
        }
        catch (ArgumentException error)
        {
            return Results.Problem(statusCode: 400, title: error.Message);
        }
        catch (Exception error) when (IsUnavailable(error))
        {
            return Unavailable();
        }
    }

    private static bool IsUnavailable(Exception error) =>
        error is SqlException or InvalidOperationException or TimeoutException;

    private static IResult Unavailable() =>
        Results.Problem(statusCode: 503, title: "El informe no está disponible.");
}
