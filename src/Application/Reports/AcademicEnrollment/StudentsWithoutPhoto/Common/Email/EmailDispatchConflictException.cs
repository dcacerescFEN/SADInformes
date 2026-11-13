namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public sealed class EmailDispatchConflictException(string message) : Exception(message);
