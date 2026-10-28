namespace Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudents;

/// <summary>Course taken by a student of the selected entry semester.</summary>
public sealed record CoursesByStudentsDTO(
    string Period, string Course, string Name, string StudentCode, string StudentName, string EntrySemester);
