using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefensesFilters;

public sealed record GetThesisDefensesFilters(string? StudentType)
    : IRequest<ApiResponse<ThesisDefensesFiltersDTO>>;

public sealed class GetThesisDefensesFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetThesisDefensesFilters, ApiResponse<ThesisDefensesFiltersDTO>>
{
    public async Task<ApiResponse<ThesisDefensesFiltersDTO>> Handle(
        GetThesisDefensesFilters request, CancellationToken ct) =>
        responses.Success(await ThesisDefensesCatalog.ReadAsync(procedures, request.StudentType, ct));
}
