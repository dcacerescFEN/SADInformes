namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;

public sealed class TeacherAvailabilityEmailConflictException(string message) : Exception(message);
