namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.StartTeacherAvailabilityEmail;

public sealed record StartTeacherAvailabilityEmailRequestDTO(string? Period, string Mode, string? TeacherCode);
