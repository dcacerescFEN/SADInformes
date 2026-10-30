using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.ThesisRegistration.Queries.GetThesisRegistrationFilters;

public sealed record GetThesisRegistrationFilters(string? StudentType)
    : IRequest<ApiResponse<ThesisRegistrationFiltersDTO>>;

public sealed class GetThesisRegistrationFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetThesisRegistrationFilters, ApiResponse<ThesisRegistrationFiltersDTO>>
{
    public async Task<ApiResponse<ThesisRegistrationFiltersDTO>> Handle(
        GetThesisRegistrationFilters request, CancellationToken ct) =>
        responses.Success(await ThesisRegistrationCatalog.ReadAsync(procedures, request.StudentType, ct));
}
