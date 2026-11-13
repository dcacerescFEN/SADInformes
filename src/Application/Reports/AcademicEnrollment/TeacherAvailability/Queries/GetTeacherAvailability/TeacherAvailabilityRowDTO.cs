namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;

public sealed record TeacherAvailabilityRowDTO(
    string Code,
    string Course,
    string TeacherCode,
    string Teacher,
    string Necessary,
    string Given,
    string Availability,
    string CreatedAt,
    string State,
    string Mail,
    string WelcomeEmail,
    string Observations);
