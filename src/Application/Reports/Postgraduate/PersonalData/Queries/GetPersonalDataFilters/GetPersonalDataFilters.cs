using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalDataFilters;

public sealed record GetPersonalDataFilters(string? StudentType) : IRequest<ApiResponse<PersonalDataFiltersDTO>>;

public sealed class GetPersonalDataFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPersonalDataFilters, ApiResponse<PersonalDataFiltersDTO>>
{
    public async Task<ApiResponse<PersonalDataFiltersDTO>> Handle(
        GetPersonalDataFilters request, CancellationToken ct) =>
        responses.Success(await PersonalDataCatalog.ReadAsync(procedures, request.StudentType, ct));
}
