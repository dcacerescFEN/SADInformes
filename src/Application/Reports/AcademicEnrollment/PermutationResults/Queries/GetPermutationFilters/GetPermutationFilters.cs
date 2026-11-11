using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationFilters;

public sealed record GetPermutationFilters : IRequest<ApiResponse<PermutationFiltersDTO>>;

public sealed class GetPermutationFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPermutationFilters, ApiResponse<PermutationFiltersDTO>>
{
    public async Task<ApiResponse<PermutationFiltersDTO>> Handle(
        GetPermutationFilters request, CancellationToken cancellationToken)
    {
        var data = await procedures.SpRec_PeriodosActivosTomaRamosAsync(
            cancellationToken: cancellationToken) ?? [];

        var periods = data.Select(row => row.Periodo.Trim())
            .Where(period => period.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(period => new PermutationPeriodOptionDTO(
                period,
                data.First(row => string.Equals(row.Periodo.Trim(), period, StringComparison.OrdinalIgnoreCase))
                    .PeriodoLargo?.Trim() is { Length: > 0 } label ? label : period))
            .ToArray();

        return responses.Success(new PermutationFiltersDTO(periods));
    }
}
