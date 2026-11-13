namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public sealed record EmailDispatchSnapshot(
    Guid JobId,
    string State,
    string Phase,
    int Total,
    int Sent,
    int Omitted,
    int Failed);
