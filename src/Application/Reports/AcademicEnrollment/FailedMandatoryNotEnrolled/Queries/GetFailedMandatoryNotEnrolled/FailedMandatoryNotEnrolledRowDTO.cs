namespace Application.Reports.AcademicEnrollment.FailedMandatoryNotEnrolled.Queries.GetFailedMandatoryNotEnrolled;

public sealed record FailedMandatoryNotEnrolledRowDTO(
    string StudentCode,
    string Name,
    string StudentType,
    string CourseCode,
    string CourseName);
