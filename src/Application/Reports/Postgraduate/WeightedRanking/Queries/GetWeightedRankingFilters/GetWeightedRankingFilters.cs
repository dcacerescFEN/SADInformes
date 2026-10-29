using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRankingFilters;

public sealed record GetWeightedRankingFilters(string? StudentType)
    : IRequest<ApiResponse<WeightedRankingFiltersDTO>>;

public sealed class GetWeightedRankingFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetWeightedRankingFilters, ApiResponse<WeightedRankingFiltersDTO>>
{
    public async Task<ApiResponse<WeightedRankingFiltersDTO>> Handle(
        GetWeightedRankingFilters request, CancellationToken ct) =>
        responses.Success(await WeightedRankingCatalog.ReadAsync(procedures, request.StudentType, ct));
}
