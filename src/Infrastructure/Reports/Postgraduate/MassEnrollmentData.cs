using Application.Common.Interfaces;
using Application.Reports.Postgraduate.MassEnrollment.Common;

namespace Infrastructure.Reports.Postgraduate;

public sealed class MassEnrollmentData(ISadContext context, ISadContextProcedures procedures) : IMassEnrollmentData
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

    public async Task<EnrollmentOption?> FindCourseAsync(EnrollmentSelection selection, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        var course = await context.FindMassEnrollmentCourseAsync(selection.Period, parts[0], parts[1], parts[2],
            selection.StudentType, selection.Modality, ct);
        return course is null ? null : new EnrollmentOption(course.Cod_Catedra.Trim(), course.NombreCatedra?.Trim() ?? course.Cod_Catedra);
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

    public Task<IEnrollmentTransaction> BeginAsync(CancellationToken ct) =>
        context.BeginEnrollmentTransactionAsync(ct);

    public Task<bool> IsEnrolledAsync(string student, EnrollmentSelection selection, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        return context.IsEnrollmentConfirmedAsync(student, selection.Period, parts[0], parts[1], ct);
    }

    public Task AddAsync(string student, EnrollmentSelection selection, string user, string pc, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        return procedures.SpCre_MovInscAdministrativo_PostgradoAsync(
            student, selection.Period, parts[0], parts[1], "AGREGA", pc, "NO", "NA", parts[2],
            0, 0, "", "PENDIENTE", "", 0, user, null, null, "SADInformes", ct);
    }

    private static string Default(string value, EnrollmentOption[] options) =>
        string.IsNullOrEmpty(value) ? options.FirstOrDefault()?.Value ?? "" : value;
    private static bool Concrete(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Todos", StringComparison.OrdinalIgnoreCase) && value != "%";
}
