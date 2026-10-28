using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Entities.Sad.Sp;

namespace Application.Common.Interfaces;

/// <summary>Database operations shared by SAD readers, procedures and administrative enrollment.</summary>
public partial interface ISadContext
{
    Task<SpRecEnrollmentCoursesResult?> FindMassEnrollmentCourseAsync(
        string period, string course, string section, string courseType, string studentType, string modality, CancellationToken ct);
    Task<int> ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken ct);
    Task<IEnrollmentTransaction> BeginEnrollmentTransactionAsync(CancellationToken ct);
    Task<bool> IsEnrollmentConfirmedAsync(string student, string period, string course, string section, CancellationToken ct);
}
