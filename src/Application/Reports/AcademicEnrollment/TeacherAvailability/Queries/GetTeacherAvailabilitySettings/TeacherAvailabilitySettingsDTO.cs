using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilitySettings;

public sealed record TeacherAvailabilitySettingsDTO(
    string Period,
    string ActivePeriod,
    IReadOnlyList<TeacherAvailabilityPeriodDTO> Periods,
    string StartDate,
    string EndDate,
    string StartTime,
    string EndTime,
    string PendingEmailText,
    string LaggardEmailText);
