using System.Data;
using Domain.Entities.Sad.Sp;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

public partial class SadContextProcedures
{
    public virtual Task<List<SpRecEnrollmentYearsResult>> SpRec_AnosAsync(CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentYearsResult>(
            "EXEC dbo.spRec_Años @IncluirTodos = @include, @TipoAlumno = @type",
            [Text("include", "NO", 4), Text("type", "Todos", 30)], ct);

    public virtual Task<List<SpRecEnrollmentPeriodsResult>> SpRec_PeriodosAcademicosFiltradosAsync(string type, CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentPeriodsResult>(
            "EXEC dbo.spRec_PeriodosAcademicosFiltrados @Propiedad = @property, @Filtro = @filter, @Orden = @order",
            [Text("property", "Tipo_Alumno", 20), Text("filter", type, 100), Text("order", "DESCENDENTE", 20)], ct);

    public virtual Task<List<SpRecEnrollmentCoursesResult>> SpRec_CursosxPeriodoAsync(
        string period, string type, string modality, CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentCoursesResult>(
            "EXEC dbo.spRec_CursosxPeriodo @Periodo = @period, @Tipo_Alumno = @type, @Modalidad = @modality",
            [Text("period", period, 5), Text("type", type, 30), Text("modality", modality, 30)], ct);

    public virtual Task<List<SpRecEnrollmentCandidatesResult>> SpRec_Alumnos_insc_masiva_catedra_dispAsync(
        string type, string modality, string cohort, string course, CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentCandidatesResult>(
            "EXEC dbo.spRec_Alumnos_insc_masiva_catedra_disp @Tipo_Alumno = @type, @Modalidad = @modality, @AñoIngreso = @cohort, @Cod_Catedra = @course",
            [Text("type", type, 30), Text("modality", modality, 30), Text("cohort", cohort, 4), Text("course", course, 30)], ct);

    private static SqlParameter Text(string name, string value, int size) =>
        new("@" + name, SqlDbType.VarChar, size) { Value = value };
}
