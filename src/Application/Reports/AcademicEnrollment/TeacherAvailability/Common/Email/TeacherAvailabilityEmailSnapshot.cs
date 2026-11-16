namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;

public sealed record TeacherAvailabilityEmailSnapshot(
    Guid JobId, string State, string Phase, int Total, int Sent, int Omitted, int Failed);
