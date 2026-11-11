namespace Application.Reports.AcademicEnrollment.EnrolledWithoutRequirements.Queries.GetEnrolledWithoutRequirements;

public sealed record EnrolledWithoutRequirementsRowDTO(
    string StudentCode,
    string Name,
    string CourseCode,
    string CourseName,
    string Section,
    string Origin,
    string Reason,
    string Date,
    string User);
