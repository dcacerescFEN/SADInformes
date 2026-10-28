using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudentsFilters;

public sealed record GetCoursesByStudentsFilters(string? StudentType)
    : IRequest<ApiResponse<CoursesByStudentsFiltersDTO>>;

public sealed class GetCoursesByStudentsFiltersHandler(ICoursesByStudentsReader reader, IApiResponseService responses)
    : IRequestHandler<GetCoursesByStudentsFilters, ApiResponse<CoursesByStudentsFiltersDTO>>
{
    public async Task<ApiResponse<CoursesByStudentsFiltersDTO>> Handle(
        GetCoursesByStudentsFilters request, CancellationToken cancellationToken)
    {
        var studentType = string.IsNullOrWhiteSpace(request.StudentType) ? null
            : ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        var filters = await reader.GetFiltersAsync(studentType, cancellationToken);
        return responses.Success(new CoursesByStudentsFiltersDTO(filters.Periods, filters.StudentTypes, filters.Modalities));
    }
}
