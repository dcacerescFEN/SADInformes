using System.Data;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Reports.Postgraduate;

/// <summary>Reads SAD data for mass enrollment and owns its transactional writes.</summary>
public sealed class MassEnrollmentData(SadContext context, ISadContextProcedures procedures) : IMassEnrollmentData
{
    public async Task<MassEnrollmentFiltersDTO> GetFiltersAsync(MassEnrollmentSelection selection, string user, CancellationToken ct)
    {
        var types = await procedures.SpRec_TiposAlumnoAsync("NO", user, cancellationToken: ct) ?? [];
        var studentTypes = types.Where(row => Concrete(row.Tipo))
            .Select(row => new MassEnrollmentOptionDTO(row.Tipo.Trim(), row.Nombre?.Trim() ?? row.Tipo)).ToArray();
        var years = await procedures.SpRec_AnosAsync(ct);
        var cohorts = years.Select(row => row.Año?.Trim()).Where(Concrete).Cast<string>().Distinct().ToArray();
        var type = Default(selection.StudentType, studentTypes);
        if (!studentTypes.Any(option => option.Value == type))
            return new(studentTypes, [], cohorts, [], []);

        var modes = await procedures.SpRec_TiposAlumnosModalidadesAsync(type, cancellationToken: ct) ?? [];
        var modalities = modes.Select(row => row.Modalidad?.Trim()).Where(Concrete).Cast<string>().Distinct().ToArray();
        var periods = (await procedures.SpRec_PeriodosAcademicosFiltradosAsync(type, ct))
            .Where(row => Concrete(row.Periodo))
            .Select(row => new MassEnrollmentOptionDTO(row.Periodo!.Trim(), row.PeriodoLargo?.Trim() ?? row.Periodo)).ToArray();
        var modality = string.IsNullOrEmpty(selection.Modality) ? modalities.FirstOrDefault() ?? "" : selection.Modality;
        var period = Default(selection.Period, periods);
        var courses = !modalities.Contains(modality) || !periods.Any(option => option.Value == period)
            ? []
            : (await procedures.SpRec_CursosxPeriodoAsync(period, type, modality, ct))
                .Where(row => Concrete(row.Cod_Catedra))
                .Select(row => new MassEnrollmentOptionDTO(row.Cod_Catedra.Trim(), row.NombreCatedra?.Trim() ?? row.Cod_Catedra))
                .DistinctBy(row => row.Value).ToArray();
        return new(studentTypes, modalities, cohorts, periods, courses);
    }

    public async Task<MassEnrollmentOptionDTO?> FindCourseAsync(MassEnrollmentSelection selection, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        // Offering identity mirrors spRec_CursosxPeriodo: capacity is keyed by the selected
        // student type; the course's own type/modality do not define availability.
        var course = await (from subject in context.Catedras.AsNoTracking()
            join offeredCourse in context.Cursos.AsNoTracking() on subject.CodCatedra equals offeredCourse.CodCatedra
            join plan in context.PlanesFormacionCatedras.AsNoTracking()
                on new { offeredCourse.CodCatedra, Decreto = offeredCourse.DecretoPostgrado }
                equals new { plan.CodCatedra, plan.Decreto }
            join vacancies in context.VacantesCursos.AsNoTracking()
                on new { offeredCourse.Periodo, offeredCourse.CodCatedra, offeredCourse.CodSeccion }
                equals new { vacancies.Periodo, vacancies.CodCatedra, vacancies.CodSeccion }
            where offeredCourse.Periodo == selection.Period && subject.CodCatedra == parts[0]
                && offeredCourse.CodSeccion == parts[1] && !offeredCourse.Eliminado
                && vacancies.TipoAlumno == selection.StudentType && vacancies.VacOfrecidas > 0
                && plan.TipoCatedra == parts[2] && plan.CodPlanFormacion == "GRAL"
                && (plan.TipoCatedra == "0" || plan.TipoCatedra == "1" || plan.TipoCatedra == "2" || plan.TipoCatedra == "3")
            select new SpRecEnrollmentCoursesResult
            {
                Cod_Catedra = subject.CodCatedra + "/" + offeredCourse.CodSeccion + "/" + plan.TipoCatedra,
                NombreCatedra = subject.NomCatedra == null ? null
                    : subject.CodCatedra + "/" + offeredCourse.CodSeccion + "/" + subject.NomCatedra,
            }).Distinct().FirstOrDefaultAsync(ct);
        return course is null ? null : new MassEnrollmentOptionDTO(course.Cod_Catedra.Trim(), course.NombreCatedra?.Trim() ?? course.Cod_Catedra);
    }

    public async Task<IReadOnlyList<MassEnrollmentCandidateDTO>> GetCandidatesAsync(MassEnrollmentSelection selection, CancellationToken ct)
    {
        var rows = await procedures.SpRec_Alumnos_insc_masiva_catedra_dispAsync(
            selection.StudentType, selection.Modality, selection.Cohort, selection.Course, ct);
        // The legacy LEFT JOIN can repeat a student for earlier enrollments.
        return rows.GroupBy(row => row.Cod_Alumno.Trim()).Select(group =>
        {
            var row = group.First();
            return new MassEnrollmentCandidateDTO(group.Key, row.Nombre?.Trim() ?? "", row.Rut?.Trim() ?? "",
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

    public Task<bool> IsEnrolledAsync(string student, MassEnrollmentSelection selection, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        return context.AlumnosCatedras.AsNoTracking().AnyAsync(row =>
            row.CodAlumno == student && row.Periodo == selection.Period && row.CodCatedra == parts[0]
            && row.CodSeccion == parts[1] && row.Categoria == "CURSADA"
            && context.MovimientosInscripcions.Any(movement =>
                movement.CodAlumno == student && movement.Periodo == selection.Period
                && movement.CodCatedra == parts[0] && movement.TipoMovimiento == "AGREGA"
                && movement.Estado == "ACEPTADA"), ct);
    }

    public Task AddAsync(string student, MassEnrollmentSelection selection, string user, string pc, CancellationToken ct)
    {
        var parts = selection.Course.Split('/');
        return procedures.SpCre_MovInscAdministrativo_PostgradoAsync(
            student, selection.Period, parts[0], parts[1], "AGREGA", pc, "NO", "NA", parts[2],
            0, 0, "", "PENDIENTE", "", 0, user, null, null, "SADInformes", ct);
    }

    private static string Default(string value, MassEnrollmentOptionDTO[] options) =>
        string.IsNullOrEmpty(value) ? options.FirstOrDefault()?.Value ?? "" : value;
    private static bool Concrete(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Todos", StringComparison.OrdinalIgnoreCase) && value != "%";

    private sealed class EnrollmentTransaction(IDbContextTransaction transaction) : IEnrollmentTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        // Dispose rolls back an uncommitted outer transaction, including nested SP writes.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
