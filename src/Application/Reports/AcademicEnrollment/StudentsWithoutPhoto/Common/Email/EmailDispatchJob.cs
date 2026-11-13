namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public sealed class EmailDispatchJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public EmailDispatchStatus Status { get; set; } = EmailDispatchStatus.Queued;
    public string Phase { get; set; } = "LOADING";
    public CancellationTokenSource Cancellation { get; } = new();
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Omitted { get; set; }
    public int Failed { get; set; }
    public object Sync { get; } = new();
}
