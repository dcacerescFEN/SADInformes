using Application.Reports.Postgraduate.MassEnrollment;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Reports.Postgraduate;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class MassEnrollmentDataTests
{
    [Fact]
    public async Task Gateway_uses_authenticated_catalogue_scope_and_exact_legacy_candidate_contract()
    {
        using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().Options);
        var procedures = new Procedures(context);
        IMassEnrollmentData data = new MassEnrollmentData(context, procedures);
        var selection = new EnrollmentSelection("MBA", "PT", "2024", "2024A", "MBA00101/1/0");
        var filters = await data.GetFiltersAsync(selection, "usuario-prueba", default);
        Assert.Equal(("NO", "usuario-prueba"), procedures.TypeArguments);
        Assert.Equal("PT", Assert.Single(filters.Modalities));
        Assert.Equal("2024", Assert.Single(filters.Cohorts));
        Assert.Equal("MBA00101/1/0", Assert.Single(filters.Courses).Value);
        Assert.Equal(("2024A", "MBA", "PT"), procedures.CourseArguments);
        var row = Assert.Single(await data.GetCandidatesAsync(selection, default));
        Assert.Equal(("MBA", "PT", "2024", "MBA00101/1/0"), procedures.CandidateArguments);
        Assert.Equal(new MassEnrollmentRow("A1", "Alumno", "123", "2024A", "REGULAR", "Motivo", "D1", true, "Ingreso"), row);
    }

    private sealed class Procedures(SadContext context) : SadContextProcedures(context)
    {
        public (string?, string?) TypeArguments;
        public (string, string, string) CourseArguments;
        public (string, string, string, string) CandidateArguments;
        public override Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(string? incluirTodos, string? usuario,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            TypeArguments = (incluirTodos, usuario);
            return Task.FromResult<List<SpRecTiposAlumnoResult>?>([new() { Tipo = "MBA", Nombre = "MBA" }, new() { Tipo = "Todos" }]);
        }
        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>([new() { Modalidad = "PT" }]);
        public override Task<List<SpRecEnrollmentYearsResult>> SpRec_AnosAsync(CancellationToken ct) =>
            Task.FromResult(new List<SpRecEnrollmentYearsResult> { new() { Año = "2024" } });
        public override Task<List<SpRecEnrollmentPeriodsResult>> SpRec_PeriodosAcademicosFiltradosAsync(string type, CancellationToken ct) =>
            Task.FromResult(new List<SpRecEnrollmentPeriodsResult> { new() { Periodo = "2024A", PeriodoLargo = "Primer semestre" } });
        public override Task<List<SpRecEnrollmentCoursesResult>> SpRec_CursosxPeriodoAsync(string period, string type, string modality, CancellationToken ct)
        {
            CourseArguments = (period, type, modality);
            return Task.FromResult(new List<SpRecEnrollmentCoursesResult> { new() { Cod_Catedra = "MBA00101/1/0", NombreCatedra = "Curso" } });
        }
        public override Task<List<SpRecEnrollmentCandidatesResult>> SpRec_Alumnos_insc_masiva_catedra_dispAsync(
            string type, string modality, string cohort, string course, CancellationToken ct)
        {
            CandidateArguments = (type, modality, cohort, course);
            return Task.FromResult(new List<SpRecEnrollmentCandidatesResult> { Candidate("NO"), Candidate("SI") });
        }
        private static SpRecEnrollmentCandidatesResult Candidate(string state) => new()
        {
            Cod_Alumno = "A1 ", Nombre = "Alumno ", Rut = "123 ", Sem_Ingreso = "2024A ",
            Tipo_Ingreso = "Ingreso ", Situacion = "REGULAR ", Motivo = "Motivo ", Decreto = "D1 ", curso_catedra = state,
        };
    }
}
