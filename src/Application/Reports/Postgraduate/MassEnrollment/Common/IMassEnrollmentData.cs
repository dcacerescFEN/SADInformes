using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;

namespace Application.Reports.Postgraduate.MassEnrollment.Common;

/// <summary>Reads enrollment catalogues and owns the transactional administrative write.</summary>
public interface IMassEnrollmentData
{
    Task<MassEnrollmentFiltersDTO> GetFiltersAsync(MassEnrollmentSelection selection, string user, CancellationToken ct);
    Task<MassEnrollmentOptionDTO?> FindCourseAsync(MassEnrollmentSelection selection, CancellationToken ct);
    Task<IReadOnlyList<MassEnrollmentCandidateDTO>> GetCandidatesAsync(MassEnrollmentSelection selection, CancellationToken ct);
    Task<IEnrollmentTransaction> BeginAsync(CancellationToken ct);
    Task<bool> IsEnrolledAsync(string student, MassEnrollmentSelection selection, CancellationToken ct);
    Task AddAsync(string student, MassEnrollmentSelection selection, string user, string pc, CancellationToken ct);
}
