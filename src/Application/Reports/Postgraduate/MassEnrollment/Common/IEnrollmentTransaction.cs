namespace Application.Reports.Postgraduate.MassEnrollment.Common;

public interface IEnrollmentTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
