namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;

public sealed record TeacherAvailabilityObservationsReportDTO(
    string Period,
    TeacherAvailabilityPeriodDTO[] Periods,
    TeacherAvailabilityObservationRowDTO[] Rows);
