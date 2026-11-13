namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;

public sealed record TeacherAvailabilityReportDTO(
    string Period,
    string PeriodName,
    TeacherAvailabilityRowDTO[] Rows,
    int Pending,
    int Ok);
