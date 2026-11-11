using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.PermutationRejections.Queries.GetPermutationRejections;

public sealed record GetPermutationRejections(string? Period)
    : IRequest<ApiResponse<PermutationRejectionsReportDTO>>;

public sealed class GetPermutationRejectionsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPermutationRejections, ApiResponse<PermutationRejectionsReportDTO>>
{
    public async Task<ApiResponse<PermutationRejectionsReportDTO>> Handle(
        GetPermutationRejections request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_ResPermutaXCursoVacantesAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new PermutationRejectionRowDTO(
            Text(row.Cod_Catedra),
            Text(row.Nom_Catedra),
            Text(row.Cod_Seccion),
            Text(row.Tipo),
            row.Rechazos ?? 0)).ToArray();

        return responses.Success(new PermutationRejectionsReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
