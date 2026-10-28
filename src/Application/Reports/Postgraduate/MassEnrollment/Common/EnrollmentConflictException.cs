namespace Application.Reports.Postgraduate.MassEnrollment.Common;

public sealed class EnrollmentConflictException(string message) : Exception(message);
