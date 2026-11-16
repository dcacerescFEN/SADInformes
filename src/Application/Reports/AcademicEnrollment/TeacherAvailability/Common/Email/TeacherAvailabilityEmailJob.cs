namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;

public sealed class TeacherAvailabilityEmailJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public TeacherAvailabilityEmailStatus Status { get; set; } = TeacherAvailabilityEmailStatus.Queued;
    public string Phase { get; set; } = "LOADING";
    public CancellationTokenSource Cancellation { get; } = new();
    public string? Period { get; init; }
    public string? User { get; init; }
    public string Mode { get; init; } = "single";
    public string? TeacherCode { get; init; }
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Omitted { get; set; }
    public int Failed { get; set; }
    public object Sync { get; } = new();
}
