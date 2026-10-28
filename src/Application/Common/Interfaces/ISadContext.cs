using Application.Reports.Postgraduate.MassEnrollment.Common;

namespace Application.Common.Interfaces;

/// <summary>Database operations shared by SAD readers, procedures and administrative enrollment.</summary>
public partial interface ISadContext
{
    Task<int> ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken ct);
    Task<IEnrollmentTransaction> BeginEnrollmentTransactionAsync(CancellationToken ct);
    Task<bool> IsEnrollmentConfirmedAsync(string student, string period, string course, string section, CancellationToken ct);
}
