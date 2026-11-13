using System.Collections.Concurrent;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public sealed class EmailDispatchRegistry
{
    private readonly ConcurrentDictionary<Guid, EmailDispatchJob> _jobs = new();

    public EmailDispatchJob Create()
    {
        var job = new EmailDispatchJob();
        _jobs[job.Id] = job;
        return job;
    }

    public bool HasActiveJob => _jobs.Values.Any(job =>
        job.Status is EmailDispatchStatus.Queued or EmailDispatchStatus.Running);

    public bool TryGet(Guid id, out EmailDispatchJob job) => _jobs.TryGetValue(id, out job!);

    public void MarkRunning(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync)
        {
            if (job.Status == EmailDispatchStatus.Cancelled) return;
            job.Status = EmailDispatchStatus.Running;
            job.Phase = "LOADING";
        }
    }

    public void SetPhase(Guid id, string phase)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync)
            if (job.Status == EmailDispatchStatus.Running) job.Phase = phase;
    }

    public bool Cancel(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return false;
        lock (job.Sync)
        {
            if (job.Status is EmailDispatchStatus.Completed or EmailDispatchStatus.Partial
                or EmailDispatchStatus.Failed or EmailDispatchStatus.Cancelled) return false;
            job.Status = EmailDispatchStatus.Cancelled;
            job.Phase = "CANCELLED";
            job.Cancellation.Cancel();
            return true;
        }
    }

    public void SetTotal(Guid id, int total)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync) job.Total = total;
    }

    public void AddOmitted(Guid id, int omitted)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync) job.Omitted += omitted;
    }

    public void IncrementSent(Guid id) => Update(id, job => job.Sent++);
    public void IncrementFailed(Guid id) => Update(id, job => job.Failed++);

    public void Finish(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync)
        {
            if (job.Status == EmailDispatchStatus.Cancelled) return;
            job.Status = job.Failed == 0
                ? EmailDispatchStatus.Completed
                : job.Sent == 0 ? EmailDispatchStatus.Failed : EmailDispatchStatus.Partial;
            job.Phase = "COMPLETED";
        }
    }

    public void Fail(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync)
        {
            if (job.Status == EmailDispatchStatus.Cancelled) return;
            job.Status = EmailDispatchStatus.Failed;
            job.Phase = "FAILED";
        }
    }

    public EmailDispatchSnapshot? Snapshot(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return null;
        lock (job.Sync)
            return new(job.Id, job.Status.ToString().ToUpperInvariant(), job.Phase,
                job.Total, job.Sent, job.Omitted, job.Failed);
    }

    private void Update(Guid id, Action<EmailDispatchJob> change)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        lock (job.Sync) change(job);
    }
}
