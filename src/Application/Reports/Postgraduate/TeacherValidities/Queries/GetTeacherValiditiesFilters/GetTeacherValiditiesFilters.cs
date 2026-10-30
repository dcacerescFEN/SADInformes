using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValiditiesFilters;

public sealed record GetTeacherValiditiesFilters : IRequest<ApiResponse<TeacherValiditiesFiltersDTO>>;

public sealed class GetTeacherValiditiesFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherValiditiesFilters, ApiResponse<TeacherValiditiesFiltersDTO>>
{
    public async Task<ApiResponse<TeacherValiditiesFiltersDTO>> Handle(
        GetTeacherValiditiesFilters request, CancellationToken ct) =>
        responses.Success(await TeacherValiditiesCatalog.ReadAsync(procedures, ct));
}
