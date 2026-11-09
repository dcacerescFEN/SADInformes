using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.StudentsWithoutPriority.Queries.GetStudentsWithoutPriority;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.AcademicEnrollment;

public sealed class StudentsWithoutPriorityEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/students-without-priority")
            .RequireAuthorization(Permission.REPORT_ACCESS)
            .WithTags("Reports/AcademicEnrollment");
        group.MapGet("/report", GetReport)
            .WithSummary("Alumnos que pueden postular y no tienen prioridad");
    }

    private static async Task<IResult> GetReport(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(new GetStudentsWithoutPriority(), cancellationToken));
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException or TimeoutException)
        {
            return Results.Problem(statusCode: 503, title: "El informe no está disponible.");
        }
    }
}
