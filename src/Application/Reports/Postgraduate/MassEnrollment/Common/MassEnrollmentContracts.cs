namespace Application.Reports.Postgraduate.MassEnrollment.Common;

public sealed record EnrollmentOption(string Value, string Text);
public sealed record EnrollmentSelection(string StudentType, string Modality, string Cohort, string Period, string Course);
public sealed record MassEnrollmentFilters(
    IReadOnlyList<EnrollmentOption> StudentTypes, IReadOnlyList<string> Modalities,
    IReadOnlyList<string> Cohorts, IReadOnlyList<EnrollmentOption> Periods, IReadOnlyList<EnrollmentOption> Courses);
public sealed record MassEnrollmentRow(string StudentCode, string Name, string Rut, string EntrySemester,
    string Situation, string Reason, string Decree, bool Enrolled, string EntryType = "");
public sealed record MassEnrollmentReport(IReadOnlyList<MassEnrollmentRow> Rows, int Total, bool CanEnroll = true)
{
    public const string NoVacanciesMessage = "El curso ya no tiene vacantes.";
    public string? AvailabilityMessage => CanEnroll ? null : NoVacanciesMessage;
}
public sealed record EnrollmentRequest(EnrollmentSelection? Selection, string[]? StudentCodes, bool Confirmed);
public sealed record EnrollmentResult(int Enrolled);

/// <summary>All operations share the request-scoped database connection and transaction.</summary>
public interface IMassEnrollmentData
{
    Task<MassEnrollmentFilters> GetFiltersAsync(EnrollmentSelection selection, string user, CancellationToken ct);
    Task<EnrollmentOption?> FindCourseAsync(EnrollmentSelection selection, CancellationToken ct);
    Task<IReadOnlyList<MassEnrollmentRow>> GetCandidatesAsync(EnrollmentSelection selection, CancellationToken ct);
    Task<IEnrollmentTransaction> BeginAsync(CancellationToken ct);
    Task<bool> IsEnrolledAsync(string student, EnrollmentSelection selection, CancellationToken ct);
    Task AddAsync(string student, EnrollmentSelection selection, string user, string pc, CancellationToken ct);
}

public interface IEnrollmentTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}

public sealed class EnrollmentConflictException(string message) : Exception(message);
