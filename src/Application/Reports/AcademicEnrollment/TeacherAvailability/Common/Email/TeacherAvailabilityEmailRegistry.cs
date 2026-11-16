using System.Collections.Concurrent;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;

public sealed class TeacherAvailabilityEmailRegistry
{
    private readonly ConcurrentDictionary<Guid, TeacherAvailabilityEmailJob> _jobs = new();
    public TeacherAvailabilityEmailJob Create(TeacherAvailabilityEmailJob job)
    {
        _jobs[job.Id] = job;
        return job;
    }
    public bool HasActiveJob => _jobs.Values.Any(job =>
        job.Status is TeacherAvailabilityEmailStatus.Queued or TeacherAvailabilityEmailStatus.Running);
    public bool TryGet(Guid id, out TeacherAvailabilityEmailJob job) => _jobs.TryGetValue(id, out job!);
    public void MarkRunning(Guid id) => Update(id, job => { if (job.Status != TeacherAvailabilityEmailStatus.Cancelled) { job.Status = TeacherAvailabilityEmailStatus.Running; job.Phase = "LOADING"; } });
    public void SetPhase(Guid id, string phase) => Update(id, job => { if (job.Status == TeacherAvailabilityEmailStatus.Running) job.Phase = phase; });
    public void SetTotal(Guid id, int total) => Update(id, job => job.Total = total);
    public void AddOmitted(Guid id, int omitted) => Update(id, job => job.Omitted += omitted);
    public void IncrementSent(Guid id) => Update(id, job => job.Sent++);
    public void IncrementFailed(Guid id) => Update(id, job => job.Failed++);
    public bool Cancel(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return false;
        lock (job.Sync)
        {
            if (job.Status is TeacherAvailabilityEmailStatus.Completed or TeacherAvailabilityEmailStatus.Partial
                or TeacherAvailabilityEmailStatus.Failed or TeacherAvailabilityEmailStatus.Cancelled) return false;
            job.Status = TeacherAvailabilityEmailStatus.Cancelled;
            job.Phase = "CANCELLED";
            job.Cancellation.Cancel();
            return true;
        }
    }
    public void Finish(Guid id) => Update(id, job => { if (job.Status == TeacherAvailabilityEmailStatus.Cancelled) return; job.Status = job.Failed == 0 ? TeacherAvailabilityEmailStatus.Completed : job.Sent == 0 ? TeacherAvailabilityEmailStatus.Failed : TeacherAvailabilityEmailStatus.Partial; job.Phase = "COMPLETED"; });
    public void Fail(Guid id) => Update(id, job => { if (job.Status == TeacherAvailabilityEmailStatus.Cancelled) return; job.Status = TeacherAvailabilityEmailStatus.Failed; job.Phase = "FAILED"; });
    public TeacherAvailabilityEmailSnapshot? Snapshot(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return null;
        lock (job.Sync) return new(job.Id, job.Status.ToString().ToUpperInvariant(), job.Phase, job.Total, job.Sent, job.Omitted, job.Failed);
    }
    private void Update(Guid id, Action<TeacherAvailabilityEmailJob> change)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync) change(job);
    }
}
