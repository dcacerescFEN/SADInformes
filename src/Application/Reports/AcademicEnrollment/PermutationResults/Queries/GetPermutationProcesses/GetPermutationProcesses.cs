using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationProcesses;

public sealed record GetPermutationProcesses(string? Period)
    : IRequest<ApiResponse<PermutationProcessesDTO>>;

public sealed class GetPermutationProcessesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPermutationProcesses, ApiResponse<PermutationProcessesDTO>>
{
    public async Task<ApiResponse<PermutationProcessesDTO>> Handle(
        GetPermutationProcesses request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_ProcesosPeriodosAsync(
            "PERMUTA", period, 0, cancellationToken: cancellationToken) ?? [];

        var processes = data.Select(row => new PermutationProcessOptionDTO(
            row.IdProcesoPeriodo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.Nombre.Trim(),
            PermutationRules.FormatDate(row.FechaInicio),
            PermutationRules.FormatDate(row.FechaTermino))).ToArray();

        return responses.Success(new PermutationProcessesDTO(processes));
    }
}
