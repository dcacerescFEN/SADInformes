using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Requests.UnresolvedPermanence.Queries.GetUnresolvedPermanenceFilters;

public sealed record GetUnresolvedPermanenceFilters
    : IRequest<ApiResponse<UnresolvedPermanenceFiltersDTO>>;

public sealed class GetUnresolvedPermanenceFiltersHandler(IApiResponseService responses)
    : IRequestHandler<GetUnresolvedPermanenceFilters, ApiResponse<UnresolvedPermanenceFiltersDTO>>
{
    public Task<ApiResponse<UnresolvedPermanenceFiltersDTO>> Handle(
        GetUnresolvedPermanenceFilters request, CancellationToken cancellationToken) =>
        Task.FromResult(responses.Success(new UnresolvedPermanenceFiltersDTO(
        [
            new("IC", "Ingeniería Comercial"),
            new("IICG", "Ingeniería en Información y Control de Gestión"),
            new("CA", "Contador Auditor"),
        ])));
}
