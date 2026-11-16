namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.UpdateTeacherAvailabilitySettings;

public sealed record UpdateTeacherAvailabilitySettingsRequestDTO(
    string? Period,
    string? StartDate,
    string? EndDate,
    string? StartTime,
    string? EndTime,
    string? PendingEmailText,
    string? LaggardEmailText,
    bool ProcessPeriod);
