namespace Application.Reports.Postgraduate.TeacherCourses.Queries.GetTeacherCoursesFilters;

public sealed record TeacherCoursesFiltersDTO(
    IReadOnlyList<TeacherCoursesOptionDTO> Years,
    IReadOnlyList<TeacherCoursesOptionDTO> Departments);
