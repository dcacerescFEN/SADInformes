namespace Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudents;

public sealed record CoursesByStudentsReportDTO(IReadOnlyList<CoursesByStudentsDTO> Rows, int Total);
