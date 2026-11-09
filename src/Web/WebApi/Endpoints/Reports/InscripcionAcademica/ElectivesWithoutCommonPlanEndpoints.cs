using Application.Common.Mediator;
using Application.Reports.InscripcionAcademica.ElectivesWithoutCommonPlan.Queries.GetElectivesWithoutCommonPlan;
using Domain.Constants;
using Microsoft.Data.SqlClient;
using WebApi.Extension;

namespace WebApi.Endpoints.Reports.InscripcionAcademica;

public sealed class ElectivesWithoutCommonPlanEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/academic-enrollment/electives-without-common-plan")
            .RequireAuthorization(Permission.REPORT_ACCESS)
            .WithTags("Reports/InscripcionAcademica");
        group.MapGet("/report", GetReport)
            .WithSummary("Electivos de mención que no están en el plan común");
    }

    private static async Task<IResult> GetReport(
        IRequestDispatcher sender, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await sender.Send(new GetElectivesWithoutCommonPlan(), cancellationToken));
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException or TimeoutException)
        {
            return Results.Problem(statusCode: 503, title: "El informe no está disponible.");
        }
    }
}
