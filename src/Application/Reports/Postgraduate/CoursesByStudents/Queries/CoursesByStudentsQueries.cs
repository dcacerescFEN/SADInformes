using Application.Reports.Postgraduate.TeacherSurvey.Queries;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.CoursesByStudents;
using Domain.Common;

namespace Application.Reports.Postgraduate.CoursesByStudents.Queries;

public sealed record CoursesByStudentsFiltersDto(
    IReadOnlyList<CoursesByStudentsOption> Periods,
    IReadOnlyList<CoursesByStudentsOption> StudentTypes,
    IReadOnlyList<string> Modalities);

public sealed record CoursesByStudentsReportDto(
    IReadOnlyList<CoursesByStudentsRow> Rows,
    int Total);

public sealed record GetCoursesByStudentsFilters(string? StudentType)
    : IRequest<ApiResponse<CoursesByStudentsFiltersDto>>;

public sealed record GetCoursesByStudents(string Period, string StudentType, string Modality)
    : IRequest<ApiResponse<CoursesByStudentsReportDto>>;

public sealed class GetCoursesByStudentsFiltersHandler(ICoursesByStudentsReader reader, IApiResponseService responses)
    : IRequestHandler<GetCoursesByStudentsFilters, ApiResponse<CoursesByStudentsFiltersDto>>
{
    public async Task<ApiResponse<CoursesByStudentsFiltersDto>> Handle(
        GetCoursesByStudentsFilters request, CancellationToken cancellationToken)
    {
        var studentType = string.IsNullOrWhiteSpace(request.StudentType) ? null
            : TeacherSurveyFiltersValidation.Required("Seleccione un tipo de alumno.", request.StudentType);
        var filters = await reader.GetFiltersAsync(studentType, cancellationToken);
        return responses.Success(new CoursesByStudentsFiltersDto(filters.Periods, filters.StudentTypes, filters.Modalities));
    }
}

public sealed class GetCoursesByStudentsHandler(ICoursesByStudentsReader reader, IApiResponseService responses)
    : IRequestHandler<GetCoursesByStudents, ApiResponse<CoursesByStudentsReportDto>>
{
    public async Task<ApiResponse<CoursesByStudentsReportDto>> Handle(
        GetCoursesByStudents request, CancellationToken cancellationToken)
    {
        var period = TeacherSurveyFiltersValidation.Required("El período es obligatorio.", request.Period);
        var studentType = TeacherSurveyFiltersValidation.Required("Seleccione un tipo de alumno.", request.StudentType);
        var modality = TeacherSurveyFiltersValidation.Required("Seleccione una modalidad.", request.Modality);

        var rows = await reader.GetRowsAsync(period, studentType, modality, cancellationToken);
        return responses.Success(new CoursesByStudentsReportDto(rows, rows.Count));
    }
}
