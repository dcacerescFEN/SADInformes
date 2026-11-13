namespace Application.Reports.AcademicEnrollment.CourseLoadReduction.Queries.GetCourseLoadReduction;

public sealed record CourseLoadReductionRowDTO(
    string Period,
    string CourseCode,
    string Section,
    string CourseName,
    string StudentCode,
    string PersonCode,
    string Decree,
    string Name,
    string Origin,
    string MovementType,
    string State);
