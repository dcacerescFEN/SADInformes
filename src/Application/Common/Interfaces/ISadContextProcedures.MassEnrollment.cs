using Domain.Entities.Sad.Sp;

namespace Application.Common.Interfaces;

public partial interface ISadContextProcedures
{
    Task<List<SpRecEnrollmentYearsResult>> SpRec_AnosAsync(CancellationToken ct);
    Task<List<SpRecEnrollmentPeriodsResult>> SpRec_PeriodosAcademicosFiltradosAsync(string type, CancellationToken ct);
    Task<List<SpRecEnrollmentCoursesResult>> SpRec_CursosxPeriodoAsync(string period, string type, string modality, CancellationToken ct);
    Task<List<SpRecEnrollmentCandidatesResult>> SpRec_Alumnos_insc_masiva_catedra_dispAsync(
        string type, string modality, string cohort, string course, CancellationToken ct);
}
