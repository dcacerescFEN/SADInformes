using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Regulation.Ranking.Queries.GetRanking;
using Application.Reports.Regulation.Ranking.Queries.GetRankingFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class RankingTests
{
    private const string FiltersRoute = "/api/reports/regulation/ranking/filters";
    private const string ReportRoute =
        "/api/reports/regulation/ranking/report?group=ICMA&period=20242&selection=PROMOCION&sortBy=RANKING&direction=ASC";
    private const string Access = "PERMISSION_REPORT_ACCESS_REPORT_MODULE";
    private const string View = "PERMISSION_REPORT_VIEW_REGULATION";

    [Fact]
    public async Task Filters_lists_distinct_academic_periods_without_touching_the_database()
    {
        var procedures = new Procedures();
        var response = await new GetRankingFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetRankingFilters(), default);

        Assert.Equal(("00000", 0, "NO", "%"),
            (procedures.ReceivedLimit, procedures.ReceivedModo, procedures.ReceivedConTodos, procedures.ReceivedTipo));
        Assert.Equal(
            new[] { ("20242", "2024 Segundo Semestre"), ("20231", "20231") },
            response.Data.Periods.Select(option => (option.Value, option.Text)).ToArray());
    }

    [Fact]
    public async Task Report_maps_every_legacy_column_and_passes_the_five_procedure_parameters()
    {
        var procedures = new Procedures();
        var response = await new GetRankingHandler(procedures, new ApiResponseService())
            .Handle(new GetRanking("icma", " 20242 ", "promocion", "nombre", "desc"), default);

        Assert.Equal(("ICMA", "20242", "PROMOCION", "NOMBRE", "DESC"),
            (procedures.ReceivedGroup, procedures.ReceivedPeriod, procedures.ReceivedSelection,
                procedures.ReceivedSortBy, procedures.ReceivedDirection));
        Assert.Equal(("ICMA", "20242", "PROMOCION", "NOMBRE", "DESC"),
            (response.Data.Group, response.Data.Period, response.Data.Selection,
                response.Data.SortBy, response.Data.Direction));

        Assert.Collection(
            response.Data.Rows,
            row => Assert.Equal(
                ("A1", "ANA PÉREZ", "3", "Decreto 3", "5.5", "2013", "20161", 1, 20),
                (row.StudentCode, row.Name, row.Decree, row.DecreeDescription, row.Average,
                    row.Promotion, row.EntrySemester, row.Ranking, row.Total)),
            row => Assert.Equal(
                ("A2", "BETO SOTO", "", "", "6", "", "", 2, 20),
                (row.StudentCode, row.Name, row.Decree, row.DecreeDescription, row.Average,
                    row.Promotion, row.EntrySemester, row.Ranking, row.Total)),
            row => Assert.Equal("", row.Average));
    }

    [Theory]
    [InlineData("OTRO", "20242", "PROMOCION", "RANKING", "ASC", "El grupo no es válido.")]
    [InlineData(null, "20242", "PROMOCION", "RANKING", "ASC", "El grupo no es válido.")]
    [InlineData("ICMA", " ", "PROMOCION", "RANKING", "ASC", "El período no es válido.")]
    [InlineData("ICMA", "202422", "PROMOCION", "RANKING", "ASC", "El período no es válido.")]
    [InlineData("ICMA", "20242", "OTRO", "RANKING", "ASC", "El tipo de selección no es válido.")]
    [InlineData("ICMA", "20242", null, "RANKING", "ASC", "El tipo de selección no es válido.")]
    [InlineData("ICMA", "20242", "PROMOCION", "OTRO", "ASC", "El orden no es válido.")]
    [InlineData("ICMA", "20242", "PROMOCION", null, "ASC", "El orden no es válido.")]
    [InlineData("ICMA", "20242", "PROMOCION", "RANKING", "OTRO", "La dirección no es válida.")]
    [InlineData("ICMA", "20242", "PROMOCION", "RANKING", null, "La dirección no es válida.")]
    public async Task Invalid_filters_are_rejected_without_calling_the_procedure(
        string? group, string? period, string? selection, string? sortBy, string? direction,
        string message)
    {
        var procedures = new Procedures();
        var handler = new GetRankingHandler(procedures, new ApiResponseService());

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new GetRanking(group, period, selection, sortBy, direction), default));

        Assert.Equal(message, error.Message);
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_rejects_requests_without_a_token()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();

        using var filters = await client.GetAsync(FiltersRoute);
        using var report = await client.GetAsync(ReportRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, filters.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, report.StatusCode);
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_rejects_the_module_permission_without_the_regulation_permission()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access));

        using var filters = await client.GetAsync(FiltersRoute);
        using var report = await client.GetAsync(ReportRoute);

        Assert.Equal(HttpStatusCode.Forbidden, filters.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, report.StatusCode);
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_returns_filters_and_the_report_envelope_and_is_not_cacheable()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));

        using var filters = await client.GetAsync(FiltersRoute);
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.True(filters.Headers.CacheControl!.NoStore);
        using var filtersJson = JsonDocument.Parse(await filters.Content.ReadAsStringAsync());
        Assert.True(filtersJson.RootElement.GetProperty("success").GetBoolean());
        var periods = filtersJson.RootElement.GetProperty("data").GetProperty("periods");
        Assert.Equal(new[] { "20242", "20231" },
            periods.EnumerateArray().Select(option => option.GetProperty("value").GetString()).ToArray());
        Assert.Equal(new[] { "2024 Segundo Semestre", "20231" },
            periods.EnumerateArray().Select(option => option.GetProperty("text").GetString()).ToArray());

        using var report = await client.GetAsync(ReportRoute);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True(report.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(new[] { "ICMA", "20242", "PROMOCION", "RANKING", "ASC" },
            new[] { "group", "period", "selection", "sortBy", "direction" }
                .Select(name => data.GetProperty(name).GetString()));
        var row = data.GetProperty("rows").EnumerateArray().First();
        Assert.Equal(new[] { "A1", "ANA PÉREZ", "3", "Decreto 3", "5.5", "2013", "20161" },
            new[] { "studentCode", "name", "decree", "decreeDescription", "average",
                    "promotion", "entrySemester" }
                .Select(name => row.GetProperty(name).GetString()));
        Assert.Equal((1, 20),
            (row.GetProperty("ranking").GetInt32(), row.GetProperty("total").GetInt32()));
        Assert.Equal(("ICMA", "20242", "PROMOCION", "RANKING", "ASC"),
            (procedures.ReceivedGroup, procedures.ReceivedPeriod, procedures.ReceivedSelection,
                procedures.ReceivedSortBy, procedures.ReceivedDirection));
    }

    [Theory]
    [InlineData("group=OTRO", "El grupo no es válido.")]
    [InlineData("group=ICMA&period=", "El período no es válido.")]
    [InlineData("group=ICMA&period=20242&selection=OTRO", "El tipo de selección no es válido.")]
    [InlineData("group=ICMA&period=20242&selection=PROMOCION&sortBy=OTRO", "El orden no es válido.")]
    [InlineData("group=ICMA&period=20242&selection=PROMOCION&sortBy=RANKING&direction=OTRO",
        "La dirección no es válida.")]
    public async Task Endpoint_rejects_invalid_filters_with_a_bad_request(string query, string message)
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));

        using var response = await client.GetAsync(
            "/api/reports/regulation/ranking/report?" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_sanitizes_procedure_failures()
    {
        var procedures = new Procedures { Fail = true };
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));

        using var report = await client.GetAsync(ReportRoute);
        using var filters = await client.GetAsync(FiltersRoute);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, report.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, filters.StatusCode);
        var body = await report.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }

    private static (WebApplicationFactory<Program>, TokenFactory) Create(Procedures procedures)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    private sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("ranking-" + Guid.NewGuid()).Options))
    {
        public int Calls { get; private set; }
        public string? ReceivedLimit { get; private set; }
        public int? ReceivedModo { get; private set; }
        public string? ReceivedConTodos { get; private set; }
        public string? ReceivedTipo { get; private set; }
        public string? ReceivedGroup { get; private set; }
        public string? ReceivedPeriod { get; private set; }
        public string? ReceivedSelection { get; private set; }
        public string? ReceivedSortBy { get; private set; }
        public string? ReceivedDirection { get; private set; }
        public bool Fail { get; init; }

        public override Task<List<SpRecPeriodosAcademicosResult>?> SpRec_PeriodosAcademicosAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Calls++;
            ReceivedLimit = limite;
            ReceivedModo = modo;
            ReceivedConTodos = conTodos;
            ReceivedTipo = tipo;
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecPeriodosAcademicosResult>?>([
                new() { Periodo = "20242", PeriodoLargo = "2024 Segundo Semestre" },
                new() { Periodo = "20231", PeriodoLargo = " " },
                new() { Periodo = " 20242 ", PeriodoLargo = "2024 Segundo Semestre" },
            ]);
        }

        public override Task<List<SpRecRankingEgresoXGrupoResult>?> SpRec_RankingEgresoXGrupoAsync(
            string? grupo, string? periodo, string? rankingPorPeriodo, string? ordenarPor, string? orden,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Calls++;
            ReceivedGroup = grupo;
            ReceivedPeriod = periodo;
            ReceivedSelection = rankingPorPeriodo;
            ReceivedSortBy = ordenarPor;
            ReceivedDirection = orden;
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecRankingEgresoXGrupoResult>?>([
                new()
                {
                    Cod_Alumno = " A1 ", Nombre = " ANA PÉREZ ", Decreto = " 3 ",
                    Decreto2 = " Decreto 3 ", Sem_Ingreso = " 20161 ", Promocion = " 2013 ",
                    Promedio = 5.5m, Ranking = 1, Total = 20,
                },
                new()
                {
                    Cod_Alumno = "A2", Nombre = "BETO SOTO", Decreto = "", Decreto2 = "",
                    Sem_Ingreso = "", Promocion = "", Promedio = 6.000m, Ranking = 2, Total = 20,
                },
                new()
                {
                    Cod_Alumno = "A3", Nombre = "CARLA DÍAZ", Promedio = null, Ranking = 3, Total = 20,
                },
            ]);
        }
    }
}
