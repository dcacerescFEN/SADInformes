using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.AyudantiaSigma.Queries.GetAyudantiaSigmaFilters;

public sealed record GetAyudantiaSigmaFilters : IRequest<ApiResponse<AyudantiaSigmaFiltersDTO>>;

public sealed class GetAyudantiaSigmaFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetAyudantiaSigmaFilters, ApiResponse<AyudantiaSigmaFiltersDTO>>
{
    public async Task<ApiResponse<AyudantiaSigmaFiltersDTO>> Handle(
        GetAyudantiaSigmaFilters request, CancellationToken ct) =>
        responses.Success(await AyudantiaSigmaCatalog.ReadAsync(procedures, ct));
}
