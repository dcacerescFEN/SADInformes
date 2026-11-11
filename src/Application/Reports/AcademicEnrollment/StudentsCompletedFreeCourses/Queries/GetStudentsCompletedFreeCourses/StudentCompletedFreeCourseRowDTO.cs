namespace Application.Reports.AcademicEnrollment.StudentsCompletedFreeCourses.Queries.GetStudentsCompletedFreeCourses;

public sealed record StudentCompletedFreeCourseRowDTO(
    string StudentCode,
    string Name,
    string Course,
    string CourseName);
