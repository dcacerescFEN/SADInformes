namespace Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudentsFilters;

public sealed record CoursesByStudentsFiltersDTO(
    IReadOnlyList<CoursesByStudentsOptionDTO> Periods,
    IReadOnlyList<CoursesByStudentsOptionDTO> StudentTypes,
    IReadOnlyList<string> Modalities);
