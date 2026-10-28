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
using Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurvey;
using Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurveyFilters;

namespace WebApi.Tests;

public sealed class TeacherSurveyTests
{
    private const string Route = "/api/reports/postgraduate/teacher-survey";

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
        var (factory, _) = Create(new FakeTeacherSurveyProcedures());
        using var _factory = factory;
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Route + "/filters?tipoAlumno=MBA")).StatusCode);
    }

    [Fact]
    public async Task Entry_permission_alone_cannot_read_report_data()
    {
        var (factory, tokens) = Create(new FakeTeacherSurveyProcedures());
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_ACCESS");
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Authorized_request_returns_shared_envelope_rows_and_total()
    {
        var reader = new FakeTeacherSurveyProcedures();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetProperty("total").GetInt32());
        var row = data.GetProperty("rows")[0];
        Assert.Equal("2024A", row.GetProperty("period").GetString());
        Assert.Equal("MBA101/1", row.GetProperty("course").GetString());
        Assert.Equal("Gestión (MBA101)", row.GetProperty("name").GetString());
        Assert.Equal("Profesora Prueba", row.GetProperty("teacherName").GetString());
        Assert.Equal(4.5m, row.GetProperty("a1").GetDecimal());
        Assert.Equal(4m, row.GetProperty("b13").GetDecimal());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("bGrade").ValueKind);
        Assert.Equal("comentario B", row.GetProperty("bComments").GetString());
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
        var reader = new FakeTeacherSurveyProcedures();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(reader.LastPeriod);
    }

    [Fact]
    public async Task Filters_are_returned_for_the_selected_student_type()
    {
        var reader = new FakeTeacherSurveyProcedures();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "/filters?tipoAlumno=MBA");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal("2024A", data.GetProperty("periods")[0].GetProperty("value").GetString());
        Assert.Equal("MBA", data.GetProperty("studentTypes")[1].GetProperty("value").GetString());
        Assert.Equal("PT", data.GetProperty("modalities")[0].GetString());
        Assert.Equal("MBA", reader.LastFilterType);
    }

    [Fact]
    public async Task Storage_failures_are_sanitized()
    {
        var (factory, tokens) = Create(new FakeTeacherSurveyProcedures { Failure = new InvalidOperationException("private-sql-detail") });
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-sql-detail", body);
        Assert.Contains("no está disponible", body);
    }

    private sealed class FakeTeacherSurveyProcedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public Exception? Failure { get; init; }
        public string? LastPeriod { get; private set; }
        public string? LastType { get; private set; }
        public string? LastModality { get; private set; }
        public string? LastFilterType { get; private set; }

        public override Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosAcademicosProgramacionResult>?>([new() { Periodo = "2024A", PeriodoLargo = "2024 - Primer Semestre" }]);

        public override Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(string? incluirTodos, string? usuario,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecTiposAlumnoResult>?>([new() { Tipo = "MGP", Nombre = "Magíster en Gestión Pública" }, new() { Tipo = "MBA", Nombre = "MBA" }]);

        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            LastFilterType = tipo;
            return Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>([new() { Modalidad = "PT" }, new() { Modalidad = "FT" }]);
        }

        public override Task<List<SpRecEncuestaDocenteXCursoResult>?> SpRec_Encuesta_DocenteXCursoAsync(
            string? periodo, string? tipo_Alumno, string? modalidad,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException<List<SpRecEncuestaDocenteXCursoResult>?>(Failure);
            LastPeriod = periodo; LastType = tipo_Alumno; LastModality = modalidad;
            return Task.FromResult<List<SpRecEncuestaDocenteXCursoResult>?>([new() { Periodo = periodo!, Curso = "MBA101/1", Catedra = "Gestión (MBA101)", Cod_Profesor = "12345678-9", Nombre_Profesor = "Profesora Prueba", Ayudantes = "Ayudante Uno", A1 = 4.5, A2 = 4, A3 = 4, A4 = 4, AObservaciones = "comentario A", B1 = 4, B2 = 4, B3 = 4, B4 = 4, B5 = 4, B6 = 4, B7 = 4, B8 = 4, B9 = 4, B10 = 4, B11 = 4, B12 = 4, B13 = 4, BNota = null, BObservaciones = "comentario B" }]);
        }
    }
}
