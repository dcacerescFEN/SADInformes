using Domain.Common;
using Domain.Entities.Sad.Sp;
using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class TeachingScheduleTests
{
    private const string Route = "/api/reports/postgraduate/teaching-schedule";
    private const string Query = "?tipoPeriodo=PER%C3%8DODO&periodo=2024B&tipoAlumno=MBA&modalidad=PT&promocion=Todas";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHING_SCHEDULE";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(ISadContextProcedures? procedures = null)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures ?? new SadContextProcedures(new SadContext(new DbContextOptions<SadContext>())));
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
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY")]
    public async Task Entry_and_sibling_report_permissions_do_not_grant_access(string role)
    {
        var (factory, tokens) = Create();
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "", "/filters" })
        {
            var response = await client.GetAsync(Route + suffix + Query);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("?tipoPeriodo=BAD&periodo=2024B&tipoAlumno=MBA")]
    [InlineData("?tipoPeriodo=SEMESTRE&periodo=&tipoAlumno=MBA")]
    [InlineData("?tipoPeriodo=A%C3%91O&periodo=2024&tipoAlumno=")]
    public async Task Invalid_required_filters_are_rejected_before_accessing_data(string query)
    {
        var (factory, tokens) = Create();
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + query)).StatusCode);
    }

    [Theory]
    [InlineData("PERÍODO", "2024B")]
    [InlineData("SEMESTRE", "20241")]
    [InlineData("AÑO", "2024")]
    public async Task Period_modes_and_long_promotion_description_are_forwarded_unchanged(string kind, string period)
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        var promotion = "Descripcion larga de promocion";
        var response = await client.GetAsync(Route + $"?tipoPeriodo={Uri.EscapeDataString(kind)}&periodo={period}&tipoAlumno=MBA&modalidad=Todas&promocion={Uri.EscapeDataString(promotion)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { period, kind, "MBA", "Todas", promotion }, procedures.Arguments);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetProperty("total").GetInt32());
        var row = data.GetProperty("rows")[0];
        Assert.Equal("MBA101/1", row.GetProperty("course").GetString());
        Assert.Equal("Cátedra", row.GetProperty("name").GetString());
        Assert.Equal("Obligatoria", row.GetProperty("type").GetString());
        Assert.Equal(3, row.GetProperty("units").GetInt32());
        Assert.Equal("Profesor A, Profesor B", row.GetProperty("teachers").GetString());
        Assert.Equal("Lunes 10:00", row.GetProperty("classes").GetString());
    }

    [Fact]
    public async Task Catalogue_preserves_promotion_display_value_and_Todas_and_scopes_periods_and_modalities()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/filters?tipoPeriodo=A%C3%91O&tipoAlumno=MBA");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("AÑO", procedures.PeriodKind);
        Assert.Equal("MBA", procedures.ModalityProgram);
        Assert.Equal("2024", data.GetProperty("periods")[0].GetProperty("value").GetString());
        Assert.Equal("Todas", data.GetProperty("modalities")[0].GetString());
        Assert.Equal("Todas", data.GetProperty("promotions")[0].GetProperty("value").GetString());
        Assert.Equal("P2024", data.GetProperty("promotions")[1].GetProperty("text").GetString());
        Assert.Equal("Descripcion larga de promocion", data.GetProperty("promotions")[1].GetProperty("value").GetString());
        Assert.Equal(new[] { "PERÍODO", "SEMESTRE", "AÑO" },
            data.GetProperty("periodTypes").EnumerateArray().Select(option => option.GetProperty("value").GetString()).ToArray());
        Assert.Equal("Bimestre", data.GetProperty("periodTypes")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task Initial_catalogue_defaults_to_bimestre_without_requesting_unscoped_modalities()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("PERÍODO", procedures.PeriodKind);
        Assert.Null(procedures.ModalityProgram);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("modalities").EnumerateArray());
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

    [Theory]
    [InlineData("")]
    [InlineData("Todas")]
    [InlineData(null)]
    public async Task Optional_filters_and_empty_results_keep_the_legacy_call(string? promotion)
    {
        var procedures = new Procedures { Empty = true };
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        var query = "?tipoPeriodo=PER%C3%8DODO&periodo=2024B&tipoAlumno=MBA";
        if (promotion is not null) query += "&promocion=" + promotion;
        using var response = await client.GetAsync(Route + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "2024B", "PERÍODO", "MBA", null, promotion }, procedures.Arguments);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetProperty("data").GetProperty("total").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("rows").EnumerateArray());
    }

    private sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public bool Fail { get; init; }
        public bool Empty { get; init; }
        public string?[]? Arguments { get; private set; }
        public string? PeriodKind { get; private set; }
        public string? ModalityProgram { get; private set; }
        public override Task<List<SpRecProgramacionDocenteXProgramaModalidadResult>?> SpRec_ProgramacionDocenteXProgramaModalidadAsync(
            string? periodo, string? tipoPeriodo, string? tipo_Alumno, string? modalidad, string? promocion,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("private-sql");
            Arguments = [periodo, tipoPeriodo, tipo_Alumno, modalidad, promocion];
            return Task.FromResult<List<SpRecProgramacionDocenteXProgramaModalidadResult>?>(Empty ? null :
                [new() { Periodo = periodo!, PeriodoLargo = "Período largo", Curso = "MBA101/1", Cod_CatedraHija = "HIJA",
                    Nom_CatedraHija = "Cátedra", Tipo = "Obligatoria", Ud = 3, Profesores = "Profesor A, Profesor B", Clases = "Lunes 10:00" }]);
        }
        public override Task<List<SpRecPeriodosAcademicosXTipoResult>?> SpRec_PeriodosAcademicosXTipoAsync(
            string? tipo, string? escuela, string? limite, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("private-sql");
            Assert.Equal(("POSTGRADO", "19000"), (escuela, limite));
            PeriodKind = tipo;
            return Task.FromResult<List<SpRecPeriodosAcademicosXTipoResult>?>([new() { Periodo = tipo == "AÑO" ? "2024" : tipo == "SEMESTRE" ? "20241" : "2024B", PeriodoLargo = "Período largo" }]);
        }
        public override Task<List<SpRecTiposAlumnoXEscuelaResult>?> SpRec_TiposAlumnoXEscuelaAsync(
            string? escuela, string? incluirTodos, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("POSTGRADO", "NO"), (escuela, incluirTodos));
            return Task.FromResult<List<SpRecTiposAlumnoXEscuelaResult>?>([new() { Tipo = "MBA", Nombre = "MBA", ORDEN = 1 }]);
        }
        public override Task<List<SpRecPromocionesPostgradoTodosResult>?> SpRec_PromocionesPostgradoTodosAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPromocionesPostgradoTodosResult>?>([
                new() { Promocion = "Todas", Descripcion = "Todas" },
                new() { Promocion = "P2024", Descripcion = "Descripcion larga de promocion" }]);
        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(
            string? tipo, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            ModalityProgram = tipo;
            return Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>([new() { Modalidad = "Todas" }, new() { Modalidad = "PT" }]);
        }
    }
}
