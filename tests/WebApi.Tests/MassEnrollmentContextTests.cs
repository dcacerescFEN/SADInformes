using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class MassEnrollmentContextTests
{
    private static async Task<SadContext> Seed()
    {
        var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Catedras.Add(new Catedra { CodCatedra = "ENECO660", NomCatedra = "Economía" });
        context.Cursos.Add(new Curso {
            Periodo = "2024A", CodCatedra = "ENECO660", CodSeccion = "02",
            TipoAlumnoPostgrado = "MBA", ModalidadPostgrado = "PT", DecretoPostgrado = "D1",
        });
        context.PlanesFormacionCatedras.Add(new PlanesFormacionCatedra {
            Decreto = "D1", CodPlanFormacion = "GRAL", CodCatedra = "ENECO660", TipoCatedra = "0",
        });
        context.VacantesCursos.Add(new VacantesCurso {
            Periodo = "2024A", CodCatedra = "ENECO660", CodSeccion = "02", TipoAlumno = "MBA", VacOfrecidas = 1,
        });
        // One accepted movement consumes the sole vacancy, but must not hide the read-only course.
        context.MovimientosInscripcions.Add(new MovimientosInscripcion {
            Identificacion = 1, CodAlumno = "A1", Periodo = "2024A", CodCatedra = "ENECO660",
            CodSeccion = "02", TipoMovimiento = "AGREGA", Estado = "ACEPTADA",
        });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task Linq_lookup_keeps_an_exhausted_course_readable_in_its_own_scope()
    {
        await using var context = await Seed();
        var result = await context.FindMassEnrollmentCourseAsync("2024A", "ENECO660", "02", "0", "MBA", "PT", default);
        Assert.NotNull(result);
        Assert.Equal("ENECO660/02/0", result.Cod_Catedra);
        Assert.Equal("ENECO660/02/Economía", result.NombreCatedra);
    }

    [Theory]
    [InlineData("2024A", "ENECO660", "02", "0", "MGP", "PT")]
    [InlineData("2024A", "ENECO660", "02", "0", "MBA", "FT")]
    [InlineData("2025A", "ENECO660", "02", "0", "MBA", "PT")]
    [InlineData("2024A", "OTHER", "02", "0", "MBA", "PT")]
    [InlineData("2024A", "ENECO660", "01", "0", "MBA", "PT")]
    [InlineData("2024A", "ENECO660", "02", "1", "MBA", "PT")]
    public async Task Linq_lookup_rejects_mismatched_course_identity_or_student_scope(
        string period, string course, string section, string type, string studentType, string modality)
    {
        await using var context = await Seed();
        Assert.Null(await context.FindMassEnrollmentCourseAsync(period, course, section, type, studentType, modality, default));
    }

    [Fact]
    public async Task Linq_lookup_rejects_deleted_and_unoffered_courses()
    {
        await using var context = await Seed();
        var course = await context.Cursos.SingleAsync();
        course.Eliminado = true;
        await context.SaveChangesAsync();
        Assert.Null(await context.FindMassEnrollmentCourseAsync("2024A", "ENECO660", "02", "0", "MBA", "PT", default));
        course.Eliminado = false;
        (await context.VacantesCursos.SingleAsync()).VacOfrecidas = 0;
        await context.SaveChangesAsync();
        Assert.Null(await context.FindMassEnrollmentCourseAsync("2024A", "ENECO660", "02", "0", "MBA", "PT", default));
    }

    [Fact]
    public async Task Linq_postcondition_requires_both_CURSADA_row_and_accepted_add()
    {
        await using var context = await Seed();
        Assert.False(await context.IsEnrollmentConfirmedAsync("A1", "2024A", "ENECO660", "02", default));
        context.AlumnosCatedras.Add(new AlumnosCatedra {
            CodAlumno = "A1", Periodo = "2024A", CodCatedra = "ENECO660", CodSeccion = "02", Categoria = "CURSADA",
        });
        await context.SaveChangesAsync();
        Assert.True(await context.IsEnrollmentConfirmedAsync("A1", "2024A", "ENECO660", "02", default));
        (await context.MovimientosInscripcions.SingleAsync()).Estado = "ELIMINADA";
        await context.SaveChangesAsync();
        Assert.False(await context.IsEnrollmentConfirmedAsync("A1", "2024A", "ENECO660", "02", default));
    }
}
