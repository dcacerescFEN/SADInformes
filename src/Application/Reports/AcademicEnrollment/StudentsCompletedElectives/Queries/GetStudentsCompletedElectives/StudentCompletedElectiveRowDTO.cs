namespace Application.Reports.AcademicEnrollment.StudentsCompletedElectives.Queries.GetStudentsCompletedElectives;

public sealed record StudentCompletedElectiveRowDTO(
    string StudentCode,
    string Name,
    string Course,
    string CourseName,
    string Decree,
    string CourseType);
