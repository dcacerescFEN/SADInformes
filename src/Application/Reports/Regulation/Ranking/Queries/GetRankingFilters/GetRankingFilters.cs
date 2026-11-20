using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Regulation.Ranking.Queries.GetRankingFilters;

public sealed record GetRankingFilters : IRequest<ApiResponse<RankingFiltersDTO>>;

public sealed class GetRankingFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetRankingFilters, ApiResponse<RankingFiltersDTO>>
{
    public async Task<ApiResponse<RankingFiltersDTO>> Handle(
        GetRankingFilters request, CancellationToken cancellationToken)
    {
        // Same defaults as the legacy call spRec_PeriodosAcademicos '00000':
        // a null @Tipo makes the SP return no rows.
        var data = await procedures.SpRec_PeriodosAcademicosAsync(
            "00000", 0, "NO", "%", cancellationToken: cancellationToken) ?? [];

        var periods = data
            .Select(row => (Value: row.Periodo?.Trim() ?? "", Label: row.PeriodoLargo?.Trim() ?? ""))
            .Where(row => row.Value.Length > 0)
            .DistinctBy(row => row.Value, StringComparer.OrdinalIgnoreCase)
            .Select(row => new RankingPeriodOptionDTO(
                row.Value, row.Label.Length > 0 ? row.Label : row.Value))
            .ToArray();

        return responses.Success(new RankingFiltersDTO(periods));
    }
}
