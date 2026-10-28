using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Application.Reports.Postgraduate.MassEnrollment;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class MassEnrollmentContextTests
{
    private static SadContext Context() => new(new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MassEnrollmentSelection Selection(string period, string course, string section, string type, string studentType) =>
        new(studentType, "PT", "2024", period, course + "/" + section + "/" + type);

    private static async Task<SadContext> Seed(
        string period = "2024A", string? courseStudentType = "MBA", string? courseModality = "PT",
        string vacancyStudentType = "MBA")
    {
        var context = Context();
        context.Catedras.Add(new Catedra { CodCatedra = "ENECO660", NomCatedra = "Economía" });
        context.Cursos.Add(new Curso {
            Periodo = period, CodCatedra = "ENECO660", CodSeccion = "02",
            TipoAlumnoPostgrado = courseStudentType, ModalidadPostgrado = courseModality,
            DecretoPostgrado = "D1",
        });
        context.PlanesFormacionCatedras.Add(new PlanesFormacionCatedra {
            Decreto = "D1", CodPlanFormacion = "GRAL", CodCatedra = "ENECO660", TipoCatedra = "0",
        });
        context.VacantesCursos.Add(new VacantesCurso {
            Periodo = period, CodCatedra = "ENECO660", CodSeccion = "02",
            TipoAlumno = vacancyStudentType, VacOfrecidas = 1,
        });
        context.MovimientosInscripcions.Add(new MovimientosInscripcion {
            Identificacion = 1, CodAlumno = "A1", Periodo = period, CodCatedra = "ENECO660",
            CodSeccion = "02", TipoMovimiento = "AGREGA", Estado = "ACEPTADA",
        });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task Data_gateway_keeps_an_exhausted_course_readable_in_its_own_scope()
    {
        await using var context = await Seed();
        var result = await MassEnrollmentLookup.FindCourseAsync(context,
            Selection("2024A", "ENECO660", "02", "0", "MBA"), default);
        Assert.NotNull(result);
        Assert.Equal("ENECO660/02/0", result.Value);
        Assert.Equal("ENECO660/02/Economía", result.Text);
    }

    [Theory]
    [InlineData("2026E", "ENECO600", "02", "1", "MBA", "MGP", "GLOBAL")]
    [InlineData("2026E", "ENECO660", "02", "0", "MBA", "MGP", null)]
    [InlineData("2026E", "ENECO660", "02", "0", "MBA", null, null)]
    [InlineData("2026E", "ENECO660", "02", "0", "MBA", "MBA", "FT")]
    public async Task Offering_identity_uses_the_type_keyed_vacancy_not_course_type_or_modality(
        string period, string course, string section, string type, string studentType,
        string? courseStudentType, string? courseModality)
    {
        await using var context = Context();
        context.Catedras.Add(new Catedra { CodCatedra = course, NomCatedra = "Economía" });
        context.Cursos.Add(new Curso {
            Periodo = period, CodCatedra = course, CodSeccion = section,
            TipoAlumnoPostgrado = courseStudentType, ModalidadPostgrado = courseModality,
            DecretoPostgrado = "D1",
        });
        context.PlanesFormacionCatedras.Add(new PlanesFormacionCatedra {
            Decreto = "D1", CodPlanFormacion = "GRAL", CodCatedra = course, TipoCatedra = type,
        });
        context.VacantesCursos.Add(new VacantesCurso {
            Periodo = period, CodCatedra = course, CodSeccion = section,
            TipoAlumno = studentType, VacOfrecidas = 1,
        });
        await context.SaveChangesAsync();
        var result = await MassEnrollmentLookup.FindCourseAsync(context,
            Selection(period, course, section, type, studentType), default);
        Assert.NotNull(result);
        Assert.Equal(course + "/" + section + "/" + type, result.Value);
    }

    [Fact]
    public async Task The_removed_course_type_modality_predicate_reproduces_the_null()
    {
        await using var context = Context();
        context.Catedras.Add(new Catedra { CodCatedra = "ENECO600", NomCatedra = "Economía" });
        context.Cursos.Add(new Curso {
            Periodo = "2026E", CodCatedra = "ENECO600", CodSeccion = "02",
            TipoAlumnoPostgrado = "MGP", ModalidadPostgrado = "GLOBAL", DecretoPostgrado = "D1",
        });
        context.PlanesFormacionCatedras.Add(new PlanesFormacionCatedra {
            Decreto = "D1", CodPlanFormacion = "GRAL", CodCatedra = "ENECO600", TipoCatedra = "1",
        });
        context.VacantesCursos.Add(new VacantesCurso {
            Periodo = "2026E", CodCatedra = "ENECO600", CodSeccion = "02",
            TipoAlumno = "MBA", VacOfrecidas = 1,
        });
        await context.SaveChangesAsync();

        // Exact former predicate: the course's own type and modality were compared with the student scope.
        var former = await (from course in context.Cursos.AsNoTracking()
            join vacancy in context.VacantesCursos.AsNoTracking()
                on new { course.Periodo, course.CodCatedra, course.CodSeccion }
                equals new { vacancy.Periodo, vacancy.CodCatedra, vacancy.CodSeccion }
            where course.Periodo == "2026E" && course.CodCatedra == "ENECO600" && course.CodSeccion == "02"
                && course.TipoAlumnoPostgrado == "MBA" && course.ModalidadPostgrado == "ANTOFAGASTA"
                && vacancy.TipoAlumno == "MBA"
            select course).FirstOrDefaultAsync();
        Assert.Null(former);

        var corrected = await MassEnrollmentLookup.FindCourseAsync(context,
            Selection("2026E", "ENECO600", "02", "1", "MBA"), default);
        Assert.NotNull(corrected);
        Assert.Equal("ENECO600/02/1", corrected.Value);
    }

    [Theory]
    [InlineData("2024A", "ENECO660", "02", "0", "MGP")]
    [InlineData("2025A", "ENECO660", "02", "0", "MBA")]
    [InlineData("2024A", "OTHER", "02", "0", "MBA")]
    [InlineData("2024A", "ENECO660", "01", "0", "MBA")]
    [InlineData("2024A", "ENECO660", "02", "1", "MBA")]
    public async Task Lookup_rejects_wrong_student_type_or_course_identity(
        string period, string course, string section, string type, string studentType)
    {
        await using var context = await Seed();
        Assert.Null(await MassEnrollmentLookup.FindCourseAsync(context,
            Selection(period, course, section, type, studentType), default));
    }

    [Fact]
    public async Task Lookup_rejects_deleted_and_unoffered_courses()
    {
        await using var context = await Seed();
        ISadContext data = context;
        var selection = Selection("2024A", "ENECO660", "02", "0", "MBA");
        var course = await context.Cursos.SingleAsync();
        course.Eliminado = true;
        await context.SaveChangesAsync();
        Assert.Null(await MassEnrollmentLookup.FindCourseAsync(data, selection, default));
        course.Eliminado = false;
        (await context.VacantesCursos.SingleAsync()).VacOfrecidas = 0;
        await context.SaveChangesAsync();
        Assert.Null(await MassEnrollmentLookup.FindCourseAsync(data, selection, default));
    }

    [Fact]
    public async Task Postcondition_requires_both_CURSADA_row_and_accepted_add()
    {
        await using var context = await Seed();
        ISadContext data = context;
        var selection = Selection("2024A", "ENECO660", "02", "0", "MBA");
        Assert.False(await MassEnrollmentLookup.IsEnrolledAsync(data, "A1", selection, default));
        context.AlumnosCatedras.Add(new AlumnosCatedra {
            CodAlumno = "A1", Periodo = "2024A", CodCatedra = "ENECO660", CodSeccion = "02", Categoria = "CURSADA",
        });
        await context.SaveChangesAsync();
        Assert.True(await MassEnrollmentLookup.IsEnrolledAsync(data, "A1", selection, default));
        (await context.MovimientosInscripcions.SingleAsync()).Estado = "ELIMINADA";
        await context.SaveChangesAsync();
        Assert.False(await MassEnrollmentLookup.IsEnrolledAsync(data, "A1", selection, default));
    }
}
