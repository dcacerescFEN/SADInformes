using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCoursesFilters;

public sealed record GetAreaCoursesFilters : IRequest<ApiResponse<AreaCoursesFiltersDTO>>;

public sealed class GetAreaCoursesFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetAreaCoursesFilters, ApiResponse<AreaCoursesFiltersDTO>>
{
    public async Task<ApiResponse<AreaCoursesFiltersDTO>> Handle(
        GetAreaCoursesFilters request, CancellationToken ct) =>
        responses.Success(await AreaCoursesCatalog.ReadAsync(procedures, ct));
}
