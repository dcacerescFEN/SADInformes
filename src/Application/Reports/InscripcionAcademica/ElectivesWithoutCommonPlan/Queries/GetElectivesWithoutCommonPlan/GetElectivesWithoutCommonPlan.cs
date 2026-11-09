using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.InscripcionAcademica.ElectivesWithoutCommonPlan.Queries.GetElectivesWithoutCommonPlan;

public sealed record GetElectivesWithoutCommonPlan : IRequest<ApiResponse<ElectivesWithoutCommonPlanReportDTO>>;

public sealed class GetElectivesWithoutCommonPlanHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetElectivesWithoutCommonPlan, ApiResponse<ElectivesWithoutCommonPlanReportDTO>>
{
    public async Task<ApiResponse<ElectivesWithoutCommonPlanReportDTO>> Handle(
        GetElectivesWithoutCommonPlan request, CancellationToken cancellationToken)
    {
        var data = await procedures.SpRec_ElectivosDeMencionSinPlanComunAsync(
            cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new ElectivesWithoutCommonPlanRowDTO(
            Text(row.Decreto2),
            Text(row.Cod_Catedra),
            Text(row.Nom_Catedra))).ToArray();

        return responses.Success(new ElectivesWithoutCommonPlanReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
