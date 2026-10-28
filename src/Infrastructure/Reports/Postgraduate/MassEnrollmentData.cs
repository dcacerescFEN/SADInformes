using System.Data;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.MassEnrollment;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Reports.Postgraduate;

public sealed class MassEnrollmentData(SadContext context, ISadContextProcedures procedures) : IMassEnrollmentData
{
    public async Task<MassEnrollmentFilters> GetFiltersAsync(EnrollmentSelection selection, string user, CancellationToken ct)
    {
        var types = await procedures.SpRec_TiposAlumnoAsync("NO", user, cancellationToken: ct) ?? [];
        var studentTypes = types.Where(row => Concrete(row.Tipo))
            .Select(row => new EnrollmentOption(row.Tipo.Trim(), row.Nombre?.Trim() ?? row.Tipo)).ToArray();
        var years = await procedures.SpRec_AnosAsync(ct);
        var cohorts = years.Select(row => row.Año?.Trim()).Where(Concrete).Cast<string>().Distinct().ToArray();
        var type = Default(selection.StudentType, studentTypes);
        if (!studentTypes.Any(option => option.Value == type))
            return new(studentTypes, [], cohorts, [], []);

        var modes = await procedures.SpRec_TiposAlumnosModalidadesAsync(type, cancellationToken: ct) ?? [];
        var modalities = modes.Select(row => row.Modalidad?.Trim()).Where(Concrete).Cast<string>().Distinct().ToArray();
        var periods = (await procedures.SpRec_PeriodosAcademicosFiltradosAsync(type, ct))
            .Where(row => Concrete(row.Periodo))
            .Select(row => new EnrollmentOption(row.Periodo!.Trim(), row.PeriodoLargo?.Trim() ?? row.Periodo)).ToArray();
        var modality = string.IsNullOrEmpty(selection.Modality) ? modalities.FirstOrDefault() ?? "" : selection.Modality;
        var period = Default(selection.Period, periods);
        var courses = !modalities.Contains(modality) || !periods.Any(option => option.Value == period)
            ? []
            : (await procedures.SpRec_CursosxPeriodoAsync(period, type, modality, ct))
                .Where(row => Concrete(row.Cod_Catedra))
                .Select(row => new EnrollmentOption(row.Cod_Catedra.Trim(), row.NombreCatedra?.Trim() ?? row.Cod_Catedra))
                .DistinctBy(row => row.Value).ToArray();
        return new(studentTypes, modalities, cohorts, periods, courses);
    }

    public async Task<IReadOnlyList<MassEnrollmentRow>> GetCandidatesAsync(EnrollmentSelection selection, CancellationToken ct)
    {
        var rows = await procedures.SpRec_Alumnos_insc_masiva_catedra_dispAsync(
            selection.StudentType, selection.Modality, selection.Cohort, selection.Course, ct);
        // The legacy LEFT JOIN can repeat a student for earlier enrollments.
        return rows.GroupBy(row => row.Cod_Alumno.Trim()).Select(group =>
        {
            var row = group.First();
            return new MassEnrollmentRow(group.Key, row.Nombre?.Trim() ?? "", row.Rut?.Trim() ?? "",
                row.Sem_Ingreso.Trim(), row.Situacion.Trim(), row.Motivo?.Trim() ?? "", row.Decreto.Trim(),
                group.Any(item => item.curso_catedra?.Trim() == "SI"), row.Tipo_Ingreso.Trim());
        }).ToArray();
    }

    public async Task<IEnrollmentTransaction> BeginAsync(CancellationToken ct)
    {
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await context.Database.ExecuteSqlRawAsync("SET XACT_ABORT ON;", ct);
            return new EnrollmentTransaction(transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public async Task<bool> IsEnrolledAsync(string student, EnrollmentSelection selection, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        var rows = await context.SqlQueryAsync<EnrollmentState>(
            """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Alumnos_Catedras WITH (UPDLOCK, HOLDLOCK)
                WHERE Cod_Alumno = @student AND Periodo = @period AND Cod_Catedra = @course
                  AND Cod_Seccion = @section AND Categoria = 'CURSADA'
            ) AND dbo.CatedraYaInscrita(@student, @course, @period) <> 0
            THEN 1 ELSE 0 END AS bit) AS Enrolled
            """,
            [Text("student", student, 20), Text("period", selection.Period, 20),
             Text("course", parts[0], 30), Text("section", parts[1], 2)], ct);
        return rows.Single().Enrolled;
    }

    public Task AddAsync(string student, EnrollmentSelection selection, string user, string pc, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        // Fixed administrative-add arguments mirror the legacy page, without string concatenation.
        return context.Database.ExecuteSqlRawAsync(
            """
            EXEC dbo.spCre_MovInscAdministrativo_Postgrado
              @Cod_Alumno=@student, @Periodo=@period, @Cod_Catedra=@course, @Cod_Seccion=@section,
              @Accion='AGREGA', @Pc=@pc, @Extracurricular='NO', @MencionCurso='NA', @Tipo_Catedra=@type,
              @EliminaAcademico=0, @AutorizadoPorAlumno=0, @Nota='', @Situacion='PENDIENTE',
              @ObservacionesNota='', @RegistroEspecial=0, @Usuario=@user, @CalificadorElectivo=NULL,
              @TipoCatedraSeleccionable=NULL, @SistemaProcesa='SADInformes'
            """,
            new object[] { Text("student", student, 20), Text("period", selection.Period, 20),
                Text("course", parts[0], 30), Text("section", parts[1], 2), Text("pc", pc, 100),
                Text("type", parts[2], 2), Text("user", user, 30) }, ct);
    }

    private static string Default(string value, EnrollmentOption[] options) =>
        string.IsNullOrEmpty(value) ? options.FirstOrDefault()?.Value ?? "" : value;
    private static bool Concrete(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Todos", StringComparison.OrdinalIgnoreCase) && value != "%";
    private static SqlParameter Text(string name, string value, int size) =>
        new("@" + name, SqlDbType.VarChar, size) { Value = value };

    private sealed class EnrollmentTransaction(IDbContextTransaction transaction) : IEnrollmentTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        // Dispose rolls back an uncommitted outer transaction, including nested SP writes.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    public sealed class EnrollmentState
    {
        public bool Enrolled { get; set; }
    }
}
