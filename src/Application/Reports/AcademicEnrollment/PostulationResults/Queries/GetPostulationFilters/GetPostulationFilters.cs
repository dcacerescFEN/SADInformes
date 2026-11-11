using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationFilters;

public sealed record GetPostulationFilters : IRequest<ApiResponse<PostulationFiltersDTO>>;

public sealed class GetPostulationFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPostulationFilters, ApiResponse<PostulationFiltersDTO>>
{
    public async Task<ApiResponse<PostulationFiltersDTO>> Handle(
        GetPostulationFilters request, CancellationToken cancellationToken)
    {
        var data = await procedures.SpRec_PeriodosActivosTomaRamosAsync(
            cancellationToken: cancellationToken) ?? [];

        var periods = data.Select(row => row.Periodo.Trim())
            .Where(period => period.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(period => new PostulationPeriodOptionDTO(
                period,
                data.First(row => string.Equals(row.Periodo.Trim(), period, StringComparison.OrdinalIgnoreCase))
                    .PeriodoLargo?.Trim() is { Length: > 0 } label ? label : period))
            .ToArray();

        return responses.Success(new PostulationFiltersDTO(periods));
    }
}
