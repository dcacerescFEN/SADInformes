using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPaymentFilters;

public sealed record GetBasicRightPaymentFilters(string? StudentType)
    : IRequest<ApiResponse<BasicRightPaymentFiltersDTO>>;

public sealed class GetBasicRightPaymentFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetBasicRightPaymentFilters, ApiResponse<BasicRightPaymentFiltersDTO>>
{
    public async Task<ApiResponse<BasicRightPaymentFiltersDTO>> Handle(
        GetBasicRightPaymentFilters request, CancellationToken ct) =>
        responses.Success(await BasicRightPaymentCatalog.ReadAsync(procedures, request.StudentType, ct));
}
