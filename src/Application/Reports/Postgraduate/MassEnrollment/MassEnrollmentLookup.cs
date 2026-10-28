using Application.Common.Interfaces;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;
using Domain.Entities.Sad.Sp;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.MassEnrollment;

/// <summary>Queries shared by the candidate listing and the enrollment command.</summary>
public static class MassEnrollmentLookup
{
    public static async Task<MassEnrollmentOptionDTO?> FindCourseAsync(ISadContext context, MassEnrollmentSelection selection, CancellationToken ct)
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

    public static Task<bool> IsEnrolledAsync(ISadContext context, string student, MassEnrollmentSelection selection, CancellationToken ct)
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
}
