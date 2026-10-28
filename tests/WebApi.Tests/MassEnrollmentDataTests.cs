using System.Data;
using Microsoft.Data.SqlClient;
using Application.Reports.Postgraduate.MassEnrollment.Common;
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

    [Fact]
    public async Task Gateway_executes_the_typed_procedure_on_SadContext_and_preserves_all_audit_parameters()
    {
        using var context = new CapturingContext();
        var data = new MassEnrollmentData(context, new SadContextProcedures(context));
        var selection = new EnrollmentSelection("MBA", "PT", "2024", "2024A", "MBA00101/1/0");
        using var cancellation = new CancellationTokenSource();
        await using (var transaction = await data.BeginAsync(cancellation.Token))
        {
            await data.AddAsync("A1", selection, "usuario-prueba", "127.0.0.1", cancellation.Token);
            Assert.Contains("EXEC dbo.spCre_MovInscAdministrativo_Postgrado", context.Sql);
            var parameters = context.Parameters.Cast<SqlParameter>().ToDictionary(item => item.ParameterName);
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
            Assert.Equal(cancellation.Token, context.Token);
            Assert.True(await data.IsEnrolledAsync("A1", selection, cancellation.Token));
            Assert.Equal(("A1", "2024A", "MBA00101", "1"), context.StateArguments);
            await transaction.CommitAsync(cancellation.Token);
            Assert.True(context.Batch.Committed);
        }
        Assert.True(context.Batch.Disposed);
    }

    [Fact]
    public async Task Exhausted_course_lookup_uses_authoritative_context_with_exact_period_section_and_type()
    {
        using var context = new CapturingContext
        {
            OfferedCourse = new() { Cod_Catedra = "ENECO660/02/0", NombreCatedra = "ENECO660/02 Economía" },
        };
        var data = new MassEnrollmentData(context, new SadContextProcedures(context));
        var selection = new EnrollmentSelection("MBA", "PT", "2024", "2024A", "ENECO660/02/0");
        Assert.Equal(new EnrollmentOption("ENECO660/02/0", "ENECO660/02 Economía"),
            await data.FindCourseAsync(selection, default));
        Assert.Equal(("2024A", "ENECO660", "02", "0", "MBA"), context.CourseArguments);
        context.OfferedCourse = null;
        Assert.Null(await data.FindCourseAsync(selection, default));
    }

    private sealed class CapturingContext() : SadContext(new DbContextOptionsBuilder<SadContext>().Options)
    {
        public string Sql = "";
        public object[] Parameters = [];
        public CancellationToken Token;
        public (string, string, string, string) StateArguments;
        public readonly FakeTransaction Batch = new();
        public SpRecEnrollmentCoursesResult? OfferedCourse;
        public (string, string, string, string, string) CourseArguments;
        public override Task<SpRecEnrollmentCoursesResult?> FindMassEnrollmentCourseAsync(
            string period, string course, string section, string courseType, string studentType, CancellationToken ct)
        {
            CourseArguments = (period, course, section, courseType, studentType);
            return Task.FromResult(OfferedCourse);
        }
        public override Task<int> ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken ct)
        {
            Sql = sql; Parameters = parameters; Token = ct;
            return Task.FromResult(-1); // SQL row count is deliberately not enrollment evidence.
        }
        public override Task<IEnrollmentTransaction> BeginEnrollmentTransactionAsync(CancellationToken ct) =>
            Task.FromResult<IEnrollmentTransaction>(Batch);
        public override Task<bool> IsEnrollmentConfirmedAsync(string student, string period, string course, string section, CancellationToken ct)
        {
            StateArguments = (student, period, course, section);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeTransaction : IEnrollmentTransaction
    {
        public bool Committed;
        public bool Disposed;
        public Task CommitAsync(CancellationToken ct) { Committed = true; return Task.CompletedTask; }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
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
