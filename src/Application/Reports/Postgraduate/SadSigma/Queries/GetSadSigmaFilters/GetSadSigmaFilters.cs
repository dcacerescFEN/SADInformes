using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.SadSigma.Queries.GetSadSigmaFilters;

public sealed record GetSadSigmaFilters : IRequest<ApiResponse<SadSigmaFiltersDTO>>;

public sealed class GetSadSigmaFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetSadSigmaFilters, ApiResponse<SadSigmaFiltersDTO>>
{
    public async Task<ApiResponse<SadSigmaFiltersDTO>> Handle(
        GetSadSigmaFilters request, CancellationToken ct) =>
        responses.Success(await SadSigmaCatalog.ReadAsync(procedures, ct));
}
