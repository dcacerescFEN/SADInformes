using System.Data;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Application.Reports.Postgraduate.MassEnrollment.Commands.CreateMassEnrollment;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class MassEnrollmentTests
{
    private const string Route = "/api/reports/postgraduate/mass-enrollment";
    private const string Query = "?studentType=MBA&modality=PT&cohort=2024&period=2024A&course=MBA00101/1/0";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_MASS_ENROLLMENT";
    private const string Manage = "PERMISSION_REPORT_MANAGE_POSTGRADUATE_MASS_ENROLLMENT";
    private static readonly MassEnrollmentSelection Selection = new("MBA", "PT", "2024", "2024A", "MBA00101/1/0");

    private static (WebApplicationFactory<Program>, TokenFactory) Create(FakeEnrollmentData data)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                data.Seed();
                services.AddSingleton<ISadContext>(data);
                services.AddSingleton<ISadContextProcedures>(new EnrollmentProcedures(data));
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    private static HttpClient Client(WebApplicationFactory<Program> factory, TokenFactory tokens, params string[] roles)
    {
        var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", roles));
        return client;
    }

    [Fact]
    public async Task Catalogues_and_candidates_preserve_legacy_fields_without_Todos()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View);
        var filters = await client.GetAsync(Route + "/filters" + Query);
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        var json = await filters.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Todos", json);
        Assert.Contains("MBA00101/1/0", json);
        Assert.False(string.IsNullOrWhiteSpace(data.CatalogueUser));
        var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = body.RootElement.GetProperty("data").GetProperty("rows");
        Assert.Equal(3, rows.GetArrayLength());
        Assert.Equal("Alumno Uno", rows[0].GetProperty("name").GetString());
        Assert.Equal("2024A", rows[0].GetProperty("entrySemester").GetString());
        Assert.Equal("REGULAR", rows[0].GetProperty("situation").GetString());
        Assert.Equal("D1", rows[0].GetProperty("decree").GetString());
        Assert.True(rows[2].GetProperty("enrolled").GetBoolean());
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Read_and_write_permissions_are_distinct_and_unauthenticated_requests_fail()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Route + Query)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Route + "/filters")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(Selection, ["A1"], true))).StatusCode);
        foreach (var role in new[] { "PERMISSION_REPORT_ACCESS", "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY" })
        {
            using var denied = Client(factory, tokens, role);
            Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync(Route + Query)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync(Route + "/filters")).StatusCode);
        }
        using var reader = Client(factory, tokens, View);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(Selection, ["A1"], true))).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?studentType=Todos&modality=PT&cohort=2024&period=2024A&course=MBA00101/1/0")]
    [InlineData("?studentType=MBA&modality=PT&cohort=2024&period=2024A&course=forged/1/0")]
    [InlineData("?studentType=MBA&modality=FT&cohort=2024&period=2024A&course=MBA00101/1/0")]
    public async Task Invalid_or_forged_filters_are_rejected_without_writes(string query)
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + query)).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Theory]
    [InlineData("A1", false, HttpStatusCode.BadRequest)]
    [InlineData("", true, HttpStatusCode.BadRequest)]
    [InlineData("A1,A1", true, HttpStatusCode.BadRequest)]
    [InlineData("forged", true, HttpStatusCode.Conflict)]
    [InlineData("A3", true, HttpStatusCode.Conflict)]
    public async Task Invalid_selection_leaves_enrollments_and_write_counter_unchanged(string codes, bool confirmed, HttpStatusCode expected)
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var before = await client.GetStringAsync(Route + Query);
        var response = await client.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(Selection, codes.Split(',', StringSplitOptions.RemoveEmptyEntries), confirmed));
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(before, await client.GetStringAsync(Route + Query));
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Confirmed_batch_enrolls_selected_students_and_replay_cannot_duplicate_them()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var request = new MassEnrollmentRequestDTO(Selection, ["A1", "A2"], true);
        var response = await client.PostAsJsonAsync(Route, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetProperty("data").GetProperty("enrolled").GetInt32());
        Assert.Equal(2, data.Writes);
        Assert.Equal("usuario-prueba", data.AuditUser);
        Assert.False(string.IsNullOrWhiteSpace(data.AuditPc));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Route, request)).StatusCode);
        Assert.Equal(2, data.Writes);
        using var rows = JsonDocument.Parse(await client.GetStringAsync(Route + Query));
        Assert.All(rows.RootElement.GetProperty("data").GetProperty("rows").EnumerateArray(),
            row => Assert.True(row.GetProperty("enrolled").GetBoolean()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Partial_failure_or_silent_noop_rolls_back_the_entire_batch(bool throws)
    {
        var data = new FakeEnrollmentData { FailSecond = throws, SkipSecond = !throws };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var before = await client.GetStringAsync(Route + Query);
        var response = await client.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(Selection, ["A1", "A2"], true));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("private-sql-detail", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await client.GetStringAsync(Route + Query));
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Enrollment_is_rechecked_even_when_legacy_candidate_status_is_stale()
    {
        var data = new FakeEnrollmentData { StaleStudent = "A1" };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(Selection, ["A1"], true))).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Manage_permission_without_view_permission_cannot_call_the_write_endpoint()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, Manage);
        var response = await client.PostAsJsonAsync(
            Route, new MassEnrollmentRequestDTO(Selection, ["A1"], true));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task ENECO660_02_last_vacancy_keeps_students_readable_and_rejects_more_enrollments()
    {
        var data = new FakeEnrollmentData { Course = "ENECO660/02/0", ExhaustAfterFirst = true };
        var selection = Selection with { Course = data.Course };
        var query = Query.Replace("MBA00101/1/0", data.Course);
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Route + query)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(selection, ["A1"], true))).StatusCode);
        using var filters = JsonDocument.Parse(await client.GetStringAsync(Route + "/filters" + query));
        Assert.Equal(0, filters.RootElement.GetProperty("data").GetProperty("courses").GetArrayLength());

        var response = await client.GetAsync(Route + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadAsStringAsync();
        using var report = JsonDocument.Parse(snapshot);
        var result = report.RootElement.GetProperty("data");
        Assert.Equal(3, result.GetProperty("total").GetInt32());
        Assert.False(result.GetProperty("canEnroll").GetBoolean());
        Assert.Equal("El curso ya no tiene vacantes.", result.GetProperty("availabilityMessage").GetString());
        Assert.True(result.GetProperty("rows")[0].GetProperty("enrolled").GetBoolean());
        Assert.False(result.GetProperty("rows")[1].GetProperty("enrolled").GetBoolean());
        Assert.Equal(snapshot, await client.GetStringAsync(Route + query));

        var rejected = await client.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(selection, ["A2"], true));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Contains("El curso ya no tiene vacantes.", await rejected.Content.ReadAsStringAsync());
        Assert.Equal(1, data.Writes);
        Assert.Equal(snapshot, await client.GetStringAsync(Route + query));
    }

    [Fact]
    public async Task A_batch_cannot_consume_more_than_the_single_remaining_vacancy()
    {
        var data = new FakeEnrollmentData { Course = "ENECO660/02/0", ExhaustAfterFirst = true };
        var selection = Selection with { Course = data.Course };
        var query = Query.Replace("MBA00101/1/0", data.Course);
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var before = await client.GetStringAsync(Route + query);
        var rejected = await client.PostAsJsonAsync(Route, new MassEnrollmentRequestDTO(selection, ["A1", "A2"], true));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal(0, data.Writes);
        Assert.Equal(before, await client.GetStringAsync(Route + query));
    }

    [Fact]
    public async Task Exhausted_course_read_still_validates_course_identity_and_other_filters()
    {
        var data = new FakeEnrollmentData { NoVacancies = true };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync(Route + Query.Replace("MBA00101/1/0", "forged/02/0"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync(Route + Query.Replace("studentType=MBA", "studentType=MGP"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync(Route + Query.Replace("period=2024A", "period=2025A"))).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Theory]
    [InlineData("studentType=MBA", "studentType=MGP")]
    public async Task Exhausted_course_from_another_valid_student_scope_is_rejected(string original, string otherScope)
    {
        var data = new FakeEnrollmentData
        {
            NoVacancies = true,
            StudentTypes = [new("MBA", "MBA"), new("MGP", "MGP")],
            Modalities = ["PT", "FT"],
        };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View);
        var response = await client.GetAsync(Route + Query.Replace(original, otherScope));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Los filtros seleccionados no están disponibles.", body);
        Assert.DoesNotContain("Alumno Uno", body);
        Assert.Equal(0, data.CandidateReads);
        Assert.Equal(0, data.Writes);
    }

    private sealed class FakeEnrollmentData() : SadContext(new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        public string Course { get; init; } = "MBA00101/1/0";
        public bool ExhaustAfterFirst { get; init; }
        public bool NoVacancies { get; init; }
        public IReadOnlyList<MassEnrollmentOptionDTO> StudentTypes { get; init; } = [new("MBA", "MBA")];
        public IReadOnlyList<string> Modalities { get; init; } = ["PT"];
        public int CandidateReads { get; set; }
        public bool FailSecond { get; init; }
        public bool SkipSecond { get; init; }
        public string? StaleStudent { get; init; }
        public int Writes { get; set; }
        public string? AuditUser { get; set; }
        public string? AuditPc { get; set; }
        public string? CatalogueUser { get; set; }

        public void Seed()
        {
            var parts = Course.Split('/');
            Catedras.Add(new() { CodCatedra = parts[0], NomCatedra = "Curso Uno" });
            Cursos.Add(new() { Periodo = "2024A", CodCatedra = parts[0], CodSeccion = parts[1], DecretoPostgrado = "D1" });
            PlanesFormacionCatedras.Add(new() { CodCatedra = parts[0], Decreto = "D1", CodPlanFormacion = "GRAL", TipoCatedra = parts[2] });
            VacantesCursos.Add(new() { Periodo = "2024A", CodCatedra = parts[0], CodSeccion = parts[1], TipoAlumno = "MBA", VacOfrecidas = 1 });
            MarkEnrolled("A3");
            if (StaleStudent is not null) MarkEnrolled(StaleStudent);
            SaveChanges();
        }

        public void MarkEnrolled(string student)
        {
            var parts = Course.Split('/');
            AlumnosCatedras.Add(new() { CodAlumno = student, Periodo = "2024A", CodCatedra = parts[0], CodSeccion = parts[1], Categoria = "CURSADA" });
            MovimientosInscripcions.Add(new() { CodAlumno = student, Periodo = "2024A", CodCatedra = parts[0], CodSeccion = parts[1], TipoMovimiento = "AGREGA", Estado = "ACEPTADA" });
        }

        public override Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            Assert.Equal(IsolationLevel.Serializable, isolationLevel);
            return Task.FromResult<IDbContextTransaction>(new Transaction(this));
        }

        private sealed class Transaction(FakeEnrollmentData data) : IDbContextTransaction
        {
            private readonly HashSet<string> snapshot = data.AlumnosCatedras.Select(row => row.CodAlumno).ToHashSet();
            private readonly int writes = data.Writes;
            private bool committed;
            public Guid TransactionId { get; } = Guid.NewGuid();
            public void Commit() => committed = true;
            public Task CommitAsync(CancellationToken cancellationToken = default) { Commit(); return Task.CompletedTask; }
            public void Rollback()
            {
                data.AlumnosCatedras.RemoveRange(data.AlumnosCatedras.Where(row => !snapshot.Contains(row.CodAlumno)));
                data.MovimientosInscripcions.RemoveRange(data.MovimientosInscripcions.Where(row => !snapshot.Contains(row.CodAlumno)));
                data.SaveChanges();
                data.Writes = writes;
            }
            public Task RollbackAsync(CancellationToken cancellationToken = default) { Rollback(); return Task.CompletedTask; }
            public void Dispose() { if (!committed) Rollback(); }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }

    private sealed class EnrollmentProcedures(FakeEnrollmentData data) : SadContextProcedures(data)
    {
        public override Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(string? incluirTodos, string? usuario,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            data.CatalogueUser = usuario;
            return Task.FromResult<List<SpRecTiposAlumnoResult>?>(data.StudentTypes.Select(row => new SpRecTiposAlumnoResult { Tipo = row.Value, Nombre = row.Text }).ToList());
        }
        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>(data.Modalities.Select(value => new SpRecTiposAlumnosModalidadesResult { Modalidad = value }).ToList());
        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(string? incluirTodos, string? tipoAlumno,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEnrollmentYearsResult>?>([new() { Año = "2024" }]);
        public override Task<List<SpRecEnrollmentPeriodsResult>?> SpRec_PeriodosAcademicosFiltradosAsync(string? propiedad, string? filtro, string? orden,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEnrollmentPeriodsResult>?>([new() { Periodo = "2024A", PeriodoLargo = "Primer semestre" }]);
        public override Task<List<SpRecEnrollmentCoursesResult>?> SpRec_CursosxPeriodoAsync(string? periodo, string? tipo_Alumno, string? modalidad,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEnrollmentCoursesResult>?>(data.NoVacancies || (data.ExhaustAfterFirst && data.Writes > 0)
                ? [] : [new() { Cod_Catedra = data.Course, NombreCatedra = "Curso Uno" }]);
        public override Task<List<SpRecEnrollmentCandidatesResult>?> SpRec_Alumnos_insc_masiva_catedra_dispAsync(
            string? tipo_Alumno, string? modalidad, string? añoIngreso, string? cod_Catedra,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            data.CandidateReads++;
            return Task.FromResult<List<SpRecEnrollmentCandidatesResult>?>(new[] { "A1", "A2", "A3" }.Select((id, i) =>
                new SpRecEnrollmentCandidatesResult { Cod_Alumno = id, Nombre = i == 0 ? "Alumno Uno" : id,
                    Rut = "12345678-9", Sem_Ingreso = "2024A", Situacion = "REGULAR", Motivo = "", Decreto = "D1", Tipo_Ingreso = "",
                    curso_catedra = id == "A3" || (id != data.StaleStudent && data.AlumnosCatedras.Any(row => row.CodAlumno == id)) ? "SI" : "NO" }).ToList());
        }
        public override async Task<int> SpCre_MovInscAdministrativo_PostgradoAsync(
            string? cod_Alumno, string? periodo, string? cod_Catedra, string? cod_Seccion, string? accion, string? pc,
            string? extracurricular, string? mencionCurso, string? tipo_Catedra, int? eliminaAcademico,
            int? autorizadoPorAlumno, string? nota, string? situacion, string? observacionesNota, int? registroEspecial,
            string? usuario, string? calificadorElectivo, string? tipoCatedraSeleccionable, string? sistemaProcesa,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("2024A", data.Course.Split('/')[0], data.Course.Split('/')[1], "AGREGA", "NO", "NA",
                data.Course.Split('/')[2], 0, 0, "", "PENDIENTE", "", 0, null, null, "SADInformes"),
                (periodo, cod_Catedra, cod_Seccion, accion, extracurricular, mencionCurso, tipo_Catedra,
                eliminaAcademico, autorizadoPorAlumno, nota, situacion, observacionesNota, registroEspecial,
                calificadorElectivo, tipoCatedraSeleccionable, sistemaProcesa));
            data.AuditUser = usuario; data.AuditPc = pc;
            if (cod_Alumno == "A2" && data.FailSecond) throw new InvalidOperationException("private-sql-detail");
            if (!(cod_Alumno == "A2" && data.SkipSecond))
            {
                data.MarkEnrolled(cod_Alumno!);
                await data.SaveChangesAsync(cancellationToken ?? default);
                data.Writes++;
            }
            return -1;
        }
    }
}
