using System.Data;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Reports.Postgraduate;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class MassEnrollmentDataTests
{
    private static SadContext Context() => new(new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Gateway_uses_authenticated_catalogue_scope_and_exact_legacy_candidate_contract()
    {
        using var context = Context();
        var procedures = new Procedures(context);
        IMassEnrollmentData data = new MassEnrollmentData(context, procedures);
        var selection = new MassEnrollmentSelection("MBA", "PT", "2024", "2024A", "MBA00101/1/0");
        var filters = await data.GetFiltersAsync(selection, "usuario-prueba", default);
        Assert.Equal(("NO", "usuario-prueba"), procedures.TypeArguments);
        Assert.Equal("PT", Assert.Single(filters.Modalities));
        Assert.Equal("2024", Assert.Single(filters.Cohorts));
        Assert.Equal("MBA00101/1/0", Assert.Single(filters.Courses).Value);
        Assert.Equal(("2024A", "MBA", "PT"), procedures.CourseArguments);
        var row = Assert.Single(await data.GetCandidatesAsync(selection, default));
        Assert.Equal(("MBA", "PT", "2024", "MBA00101/1/0"), procedures.CandidateArguments);
        Assert.Equal(new MassEnrollmentCandidateDTO("A1", "Alumno", "123", "2024A", "REGULAR", "Motivo", "D1", true, "Ingreso"), row);
    }

    [Fact]
    public async Task Administrative_write_delegates_all_nineteen_arguments_to_the_typed_procedure()
    {
        using var context = Context();
        var procedures = new Procedures(context);
        IMassEnrollmentData data = new MassEnrollmentData(context, procedures);
        await data.AddAsync("A1", new MassEnrollmentSelection("MBA", "PT", "2024", "2024A", "MBA00101/1/0"),
            "usuario-prueba", "127.0.0.1", default);
        Assert.Equal(("A1", "2024A", "MBA00101", "1", "AGREGA", "127.0.0.1", "NO", "NA", "0",
            0, 0, "", "PENDIENTE", "", 0, "usuario-prueba", null, null, "SADInformes", default), procedures.WriteArguments);
    }

    [Fact]
    public void Procedure_parameter_contract_preserves_exact_names_types_sizes_and_defaults()
    {
        var parameters = SadContextProcedures.BuildMovInscAdministrativoParameters(
                "A1", "2024A", "MBA00101", "1", "AGREGA", "127.0.0.1", "NO", "NA", "0", 0, 0, "",
                "PENDIENTE", "", 0, "usuario-prueba", null, null, "SADInformes")
            .ToDictionary(item => item.ParameterName);
        Assert.Equal(19, parameters.Count);
        foreach (var (name, value, size) in new (string, object, int)[] {
            ("student", "A1", 20), ("period", "2024A", 20), ("course", "MBA00101", 30), ("section", "1", 2),
            ("action", "AGREGA", 10), ("pc", "127.0.0.1", 100), ("extra", "NO", 2), ("mention", "NA", 2),
            ("type", "0", 2), ("grade", "", 5), ("situation", "PENDIENTE", 10), ("notes", "", 1000),
            ("user", "usuario-prueba", 30), ("elective", DBNull.Value, 10),
            ("selectableType", DBNull.Value, 1), ("system", "SADInformes", 15) })
        {
            Assert.Equal(value, parameters["@" + name].Value);
            Assert.Equal(size, parameters["@" + name].Size);
            Assert.Equal(SqlDbType.VarChar, parameters["@" + name].SqlDbType);
        }
        foreach (var name in new[] { "removeAcademic", "authorized", "special" })
        {
            Assert.Equal(0, parameters["@" + name].Value);
            Assert.Equal(SqlDbType.Int, parameters["@" + name].SqlDbType);
        }
    }

    private sealed class Procedures(SadContext context) : SadContextProcedures(context)
    {
        public (string?, string?) TypeArguments;
        public (string, string, string) CourseArguments;
        public (string, string, string, string) CandidateArguments;
        public (string, string, string, string, string, string, string, string, string, int, int, string,
            string, string, int, string, string?, string?, string, CancellationToken) WriteArguments;
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
        public override Task<int> SpCre_MovInscAdministrativo_PostgradoAsync(
            string codAlumno, string periodo, string codCatedra, string codSeccion, string accion, string pc,
            string extracurricular, string mencionCurso, string tipoCatedra, int eliminaAcademico,
            int autorizadoPorAlumno, string nota, string situacion, string observacionesNota, int registroEspecial,
            string usuario, string? calificadorElectivo, string? tipoCatedraSeleccionable, string sistemaProcesa,
            CancellationToken ct)
        {
            WriteArguments = (codAlumno, periodo, codCatedra, codSeccion, accion, pc, extracurricular,
                mencionCurso, tipoCatedra, eliminaAcademico, autorizadoPorAlumno, nota, situacion,
                observacionesNota, registroEspecial, usuario, calificadorElectivo, tipoCatedraSeleccionable,
                sistemaProcesa, ct);
            return Task.FromResult(-1);
        }
        private static SpRecEnrollmentCandidatesResult Candidate(string state) => new()
        {
            Cod_Alumno = "A1 ", Nombre = "Alumno ", Rut = "123 ", Sem_Ingreso = "2024A ",
            Tipo_Ingreso = "Ingreso ", Situacion = "REGULAR ", Motivo = "Motivo ", Decreto = "D1 ", curso_catedra = state,
        };
    }
}
