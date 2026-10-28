using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Tests;

public sealed class UnansweredTeacherSurveyTests
{
    private const string Route = "/api/reports/postgraduate/unanswered-teacher-survey";

    private static (WebApplicationFactory<Program> Factory, TokenFactory Tokens) Create(
        ISadContextProcedures reader, params string[] roles)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(reader);
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    private static HttpClient Authorized(WebApplicationFactory<Program> factory, TokenFactory tokens,
        params string[] roles) { var client = factory.CreateClient(); client.SetBearer(tokens.CreateToken("SADInformes", roles)); return client; }

    [Fact]
    public async Task Report_requires_authentication()
    {
        var (factory, _) = Create(new FakeUnansweredTeacherSurveyProcedures());
        using var _factory = factory;
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Route + "/filters?tipoAlumno=MBA")).StatusCode);
    }

    [Theory]
    [InlineData("PERMISSION_REPORT_ACCESS")]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY")]
    public async Task Entry_permission_alone_cannot_read_report_data(string role)
    {
        var (factory, tokens) = Create(new FakeUnansweredTeacherSurveyProcedures());
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, role);
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Route + "/filters")).StatusCode);
    }

    [Fact]
    public async Task Authorized_request_returns_shared_envelope_rows_and_total()
    {
        var reader = new FakeUnansweredTeacherSurveyProcedures();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetProperty("total").GetInt32());
        var row = data.GetProperty("rows")[0];
        Assert.Equal("2024B", row.GetProperty("period").GetString());
        Assert.Equal("MBA101/1", row.GetProperty("course").GetString());
        Assert.Equal("Gestión (MBA101)", row.GetProperty("name").GetString());
        Assert.Equal("A001", row.GetProperty("studentCode").GetString());
        Assert.Equal("Alumno Prueba", row.GetProperty("studentName").GetString());
        Assert.Equal(("2024A", "MBA", "PT"), (reader.LastPeriod, reader.LastType, reader.LastModality));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?periodo=&tipoAlumno=MBA&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=MBA&modalidad=")]
    [InlineData("?periodo=Todos&tipoAlumno=MBA&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=Todos&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=MBA&modalidad=Todos")]
    [InlineData("?periodo=%25&tipoAlumno=MBA&modalidad=PT")]
    public async Task Invalid_filters_are_rejected_instead_of_returning_an_empty_table(string query)
    {
        var reader = new FakeUnansweredTeacherSurveyProcedures();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(reader.LastPeriod);
    }

    [Fact]
    public async Task Filters_are_returned_for_the_selected_student_type()
    {
        var reader = new FakeUnansweredTeacherSurveyProcedures();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "/filters?tipoAlumno=MBA");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal("2024A", data.GetProperty("periods")[0].GetProperty("value").GetString());
        Assert.Equal("MBA", data.GetProperty("studentTypes")[1].GetProperty("value").GetString());
        Assert.Equal("PT", data.GetProperty("modalities")[0].GetString());
        Assert.Equal("MBA", reader.LastFilterType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/filters")]
    public async Task Storage_failures_are_sanitized(string suffix)
    {
        var (factory, tokens) = Create(new FakeUnansweredTeacherSurveyProcedures { Failure = new InvalidOperationException("private-sql-detail") });
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + suffix + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-sql-detail", body);
        Assert.Contains("no está disponible", body);
    }

    [Theory]
    [InlineData(20, 30, 30, HttpStatusCode.OK)]
    [InlineData(21, 30, 30, HttpStatusCode.BadRequest)]
    [InlineData(20, 31, 30, HttpStatusCode.BadRequest)]
    [InlineData(20, 30, 31, HttpStatusCode.BadRequest)]
    public async Task Sql_parameter_lengths_are_checked_before_reading(int periodLength, int typeLength, int modalityLength, HttpStatusCode expected)
    {
        var procedures = new FakeUnansweredTeacherSurveyProcedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY");
        var period = new string('A', periodLength);
        var type = new string('B', typeLength);
        var modality = new string('C', modalityLength);
        var response = await client.GetAsync(Route + $"?periodo={period}&tipoAlumno={type}&modalidad={modality}");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
            Assert.Equal((period, type, modality), (procedures.LastPeriod, procedures.LastType, procedures.LastModality));
        else Assert.Null(procedures.LastPeriod);
    }

    [Theory]
    [InlineData("ROLE_REPORT_ADMIN")]
    [InlineData("ROLE_REPORT_SUPERADMIN")]
    public async Task Existing_report_administrators_can_read_both_endpoints(string role)
    {
        var (factory, tokens) = Create(new FakeUnansweredTeacherSurveyProcedures());
        using var host = factory;
        using var signing = tokens;
        using var client = Authorized(factory, tokens, role);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Route + "/filters")).StatusCode);
    }

    [Fact]
    public async Task Initial_catalogue_has_no_unscoped_modalities_or_Todos()
    {
        var procedures = new FakeUnansweredTeacherSurveyProcedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY");
        using var response = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("modalities").EnumerateArray());
        Assert.DoesNotContain("Todos", json.RootElement.GetProperty("data").GetRawText());
        Assert.Null(procedures.LastFilterType);
        Assert.Equal("NO", procedures.IncludeAll);
    }

    [Fact]
    public async Task Invalid_catalogue_type_does_not_read_any_catalogue()
    {
        var procedures = new FakeUnansweredTeacherSurveyProcedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY");
        foreach (var type in new[] { "Todos", "%25", new string('A', 31) })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + "/filters?tipoAlumno=" + type)).StatusCode);
        Assert.Null(procedures.IncludeAll);
        Assert.Null(procedures.LastFilterType);
    }

    private sealed class FakeUnansweredTeacherSurveyProcedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public Exception? Failure { get; init; }
        public string? LastPeriod { get; private set; }
        public string? LastType { get; private set; }
        public string? LastModality { get; private set; }
        public string? LastFilterType { get; private set; }
        public string? IncludeAll { get; private set; }

        public override Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException<List<SpRecPeriodosAcademicosProgramacionResult>?>(Failure);
            Assert.Equal(("20040", 0, "NO", "POSTGRADO"), (limite, modo, conTodos, tipo));
            return Task.FromResult<List<SpRecPeriodosAcademicosProgramacionResult>?>([new() { Periodo = "2024A", PeriodoLargo = "2024 - Primer Semestre" }]);
        }

        public override Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(string? incluirTodos, string? usuario,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            IncludeAll = incluirTodos;
            return Task.FromResult<List<SpRecTiposAlumnoResult>?>([new() { Tipo = "MGP", Nombre = "Magíster en Gestión Pública" }, new() { Tipo = "MBA", Nombre = "MBA" }, new() { Tipo = "Todos" }]);
        }

        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            LastFilterType = tipo;
            return Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>([new() { Modalidad = "PT" }, new() { Modalidad = "FT" }]);
        }

        public override Task<List<SpRecAlumnosSinEvaluacionDocenteResult>?> SpRec_Alumnos_Sin_Evaluacion_DocenteAsync(
            string? periodo, string? tipo_Alumno, string? modalidad,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException<List<SpRecAlumnosSinEvaluacionDocenteResult>?>(Failure);
            LastPeriod = periodo; LastType = tipo_Alumno; LastModality = modalidad;
            return Task.FromResult<List<SpRecAlumnosSinEvaluacionDocenteResult>?>([new() { Periodo = "2024B", Curso = "MBA101/1", Cod_Catedra = " MBA101 ", Cod_Seccion = "1 ", Catedra = "Gestión (MBA101)", Cod_Alumno = "A001", Nombre = "Alumno Prueba" }]);
        }
    }
}
