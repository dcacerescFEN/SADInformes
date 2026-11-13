namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public enum EmailDispatchStatus
{
    Queued,
    Running,
    Completed,
    Partial,
    Failed,
    Cancelled,
}
