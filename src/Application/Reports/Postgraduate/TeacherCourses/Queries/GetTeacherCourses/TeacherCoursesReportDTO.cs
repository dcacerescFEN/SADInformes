namespace Application.Reports.Postgraduate.TeacherCourses.Queries.GetTeacherCourses;

public sealed record TeacherCoursesReportDTO(IReadOnlyList<TeacherCoursesDTO> Rows, int Total);
