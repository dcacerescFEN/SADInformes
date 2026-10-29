using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace WebApi.Tests;

public sealed class TeacherCoursesTests
{
    private const string Route = "/api/reports/postgraduate/teacher-courses";
    private const string Query = "?periodo=2024&departamento=Ingenier%C3%ADa";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(
        ISadContextProcedures? procedures = null, IPostgraduateSettings? settings = null)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(procedures ?? new SadContextProcedures(new SadContext(new DbContextOptions<SadContext>())));
                services.AddSingleton(settings ?? new Settings());
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/filters")]
    public async Task Both_routes_require_authentication(string suffix)
    {
        var (factory, tokens) = Create();
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Route + suffix + Query)).StatusCode);
    }

    [Theory]
    [InlineData("PERMISSION_REPORT_ACCESS")]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHING_SCHEDULE")]
    public async Task Entry_and_sibling_report_permissions_do_not_grant_access(string role)
    {
        var (factory, tokens) = Create(new Procedures());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "", "/filters" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Route + suffix + Query)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?periodo=24&departamento=Ingenier%C3%ADa")]
    [InlineData("?periodo=2024A&departamento=Ingenier%C3%ADa")]
    [InlineData("?periodo=2024&departamento=")]
    [InlineData("?periodo=2024&departamento=%20%20%20")]
    public async Task Invalid_filters_are_rejected_before_accessing_data(string query)
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + query)).StatusCode);
        Assert.Null(procedures.ReportArguments);
    }

    [Fact]
    public async Task Report_maps_the_legacy_columns_and_keeps_typed_nulls()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "2024", "Ingeniería" }, procedures.ReportArguments);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(3, data.GetProperty("total").GetInt32());
        var first = data.GetProperty("rows")[0];
        Assert.Equal("PROF001", first.GetProperty("professorCode").GetString());
        Assert.Equal("ANA PEREZ", first.GetProperty("name").GetString());
        Assert.Equal(3, first.GetProperty("evaluatedCourses").GetInt32());
        Assert.Equal("5,250000", first.GetProperty("average").GetString());
        Assert.Equal("AQ", first.GetProperty("classification").GetString());
        var second = data.GetProperty("rows")[1];
        Assert.Equal(JsonValueKind.Null, second.GetProperty("evaluatedCourses").ValueKind);
        Assert.Equal("", second.GetProperty("average").GetString());
    }

    [Fact]
    public async Task Average_is_returned_with_comma_and_without_rounding()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = json.RootElement.GetProperty("data").GetProperty("rows");
        // decimal(19,6) scale is preserved: no rounding, no trailing-zero trimming.
        Assert.Equal("5,250000", rows[0].GetProperty("average").GetString());
        Assert.Equal("4,333333", rows[2].GetProperty("average").GetString());
    }

    [Fact]
    public async Task Department_outside_the_configured_user_scope_is_rejected_without_querying_the_report()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        var response = await client.GetAsync(Route + "?periodo=2024&departamento=Otra");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(procedures.ReportArguments);
        Assert.Equal("configured-user", procedures.User);
    }

    [Fact]
    public async Task Year_outside_the_postgraduate_catalogue_is_rejected_without_querying_the_report()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        var response = await client.GetAsync(Route + "?periodo=2019&departamento=Ingenier%C3%ADa");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(procedures.ReportArguments);
    }

    [Fact]
    public async Task Catalogue_scopes_departments_with_the_configured_user_and_hides_placeholders()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("configured-user", procedures.User);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("2024", data.GetProperty("years")[0].GetProperty("value").GetString());
        Assert.Equal("2024", data.GetProperty("years")[0].GetProperty("text").GetString());
        Assert.Equal(1, data.GetProperty("departments").GetArrayLength());
        Assert.Equal("Ingeniería", data.GetProperty("departments")[0].GetProperty("value").GetString());
    }

    [Fact]
    public async Task Missing_configuration_user_returns_a_sanitized_unavailable_response()
    {
        var (factory, tokens) = Create(new Procedures(), new FailingSettings());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        foreach (var suffix in new[] { "", "/filters" })
        {
            var response = await client.GetAsync(Route + suffix + Query);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("usuario de Postgrado", body);
            Assert.Contains("El informe no está disponible.", body);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("/filters")]
    public async Task Storage_errors_are_sanitized(string suffix)
    {
        var (factory, tokens) = Create(new Procedures { Fail = true });
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        var response = await client.GetAsync(Route + suffix + Query);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-sql", body);
        Assert.Contains("El informe no está disponible.", body);
    }

    [Theory]
    [InlineData("ROLE_REPORT_ADMIN")]
    [InlineData("ROLE_REPORT_SUPERADMIN")]
    public async Task Existing_administrators_can_read_both_routes(string role)
    {
        var (factory, tokens) = Create(new Procedures());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "", "/filters" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Route + suffix + Query)).StatusCode);
    }

    [Fact]
    public async Task Empty_results_keep_the_legacy_call()
    {
        var procedures = new Procedures { Empty = true };
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "2024", "Ingeniería" }, procedures.ReportArguments);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetProperty("data").GetProperty("total").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("rows").EnumerateArray());
    }

    private sealed class Settings : IPostgraduateSettings
    {
        public string User => "configured-user";
    }

    private sealed class FailingSettings : IPostgraduateSettings
    {
        public string User => throw new InvalidOperationException("El usuario de Postgrado no está configurado.");
    }

    private sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public bool Fail { get; init; }
        public bool Empty { get; init; }
        public string?[]? ReportArguments { get; private set; }
        public string? User { get; private set; }

        public override Task<List<SpRecPeriodosAcademicosXTipoResult>?> SpRec_PeriodosAcademicosXTipoAsync(
            string? tipo, string? escuela, string? limite, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("private-sql");
            Assert.Equal(("AÑO", "POSTGRADO", "19000"), (tipo, escuela, limite));
            return Task.FromResult<List<SpRecPeriodosAcademicosXTipoResult>?>(
                [new() { Periodo = "2024", PeriodoLargo = "2024" }, new() { Periodo = "Todos" }]);
        }

        public override Task<List<SpRecDocentesDepartamentosResult>?> SpRec_DocentesDepartamentosAsync(
            string? incluirTodos, string? usuario, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("private-sql");
            Assert.Equal("NO", incluirTodos);
            User = usuario;
            return Task.FromResult<List<SpRecDocentesDepartamentosResult>?>(
                [new() { Departamento = "Ingeniería" }, new() { Departamento = "Todos" }, new() { Departamento = "*" }]);
        }

        public override Task<List<SpRecEvaPostgradoCursosDocentes2Result>?> SpRec_EvaPostgradoCursosDocentes2Async(
            string? año, string? departamento, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("private-sql");
            ReportArguments = [año, departamento];
            return Task.FromResult<List<SpRecEvaPostgradoCursosDocentes2Result>?>(Empty ? null :
                [
                    new() { Cod_Profesor = " PROF001 ", Nombre = " ANA PEREZ ", Catedras = 3, Promedio = 5.250000m, AQPQ = "AQ" },
                    new() { Cod_Profesor = "PROF002", Nombre = "LUIS SOTO", Catedras = null, Promedio = null, AQPQ = " " },
                    new() { Cod_Profesor = "PROF003", Nombre = "EVA ROJAS", Catedras = 1, Promedio = 4.333333m, AQPQ = "PQ" },
                ]);
        }
    }
}
