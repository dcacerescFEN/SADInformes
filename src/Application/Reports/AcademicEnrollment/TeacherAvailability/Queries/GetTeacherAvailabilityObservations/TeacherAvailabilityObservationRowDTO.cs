namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;

public sealed record TeacherAvailabilityObservationRowDTO(
    string TeacherCode,
    string Teacher,
    string Observations);
