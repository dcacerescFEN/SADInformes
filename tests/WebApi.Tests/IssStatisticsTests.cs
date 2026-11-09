using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsFilters;
using Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsSummary;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class IssStatisticsTests
{
    [Fact]
    public async Task Filters_exclude_Todos_and_default_to_the_first_period()
    {
        var procedures = new Procedures();
        var response = await new GetIssStatisticsFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetIssStatisticsFilters(null), default);
        Assert.Equal(new[] { "2024A", "2023B" }, response.Data.Periods.Select(row => row.Value));
        Assert.Equal("2024 - Primer Semestre", response.Data.Periods[0].Text);
        Assert.Equal(new[] { "ENECO775/1", "ENFIN716/2" },
            response.Data.CourseSections.Select(row => row.Value));
        Assert.Equal("2024A", procedures.SectionPeriod);
    }

    [Fact]
    public async Task Summary_maps_sample_categories_questions_and_observations_in_order()
    {
        using var context = CreateContext(
            new EvaluacionIssPostgradoCategoria { IdCategoria = 10, Descripcion = "Categoría diez", IdEvaluacion = 1 },
            new EvaluacionIssPostgradoCategoria { IdCategoria = 20, Descripcion = "Categoría veinte", IdEvaluacion = 1 });
        var procedures = new Procedures();
        var response = await new GetIssStatisticsSummaryHandler(procedures, context, new ApiResponseService())
            .Handle(new GetIssStatisticsSummary("2024A", "ENECO775/1"), default);
        var data = response.Data;
        Assert.Equal(("ENECO775", "1", "2024A", 10, 8, 80d, 2),
            (data.Sample!.CourseCode, data.Sample.Section, data.Sample.Period, data.Sample.Enrolled,
                data.Sample.Answered, data.Sample.Percentage, data.Sample.Missing));
        Assert.Equal(new[] { 20, 10 }, data.Categories.Select(row => row.Id));
        Assert.Equal(new[] { "Categoría veinte", "Categoría diez" },
            data.Categories.Select(row => row.Description));
        Assert.Equal(("Pregunta veinte", 1, 2, 3, 4, 5, 15, 3.5),
            (data.Categories[0].Questions[0].Question, data.Categories[0].Questions[0].Alternative1,
                data.Categories[0].Questions[0].Alternative2, data.Categories[0].Questions[0].Alternative3,
                data.Categories[0].Questions[0].Alternative4, data.Categories[0].Questions[0].Alternative5,
                data.Categories[0].Questions[0].Total, data.Categories[0].Questions[0].Average));
        Assert.Equal(new[] { "Observación uno", "Observación dos" }, data.Observations);
        Assert.Equal(new[] { 20, 10 }, procedures.StatisticsCalls);
    }

    [Fact]
    public async Task Invalid_filters_never_call_summary_procedures_and_no_sample_is_valid()
    {
        var procedures = new Procedures();
        var context = CreateContext();
        var handler = new GetIssStatisticsSummaryHandler(procedures, context, new ApiResponseService());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new GetIssStatisticsSummary("9999Z", "ENECO775/1"), default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new GetIssStatisticsSummary("2024A", "ENECO775"), default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new GetIssStatisticsSummary("2024A", "/1"), default));
        Assert.Empty(procedures.StatisticsCalls);

        procedures.NoData = true;
        var response = await handler.Handle(new GetIssStatisticsSummary("2024A", "ENECO775/1"), default);
        Assert.Null(response.Data.Sample);
        Assert.Empty(response.Data.Categories);
        Assert.Empty(response.Data.Observations);
    }

    [Fact]
    public async Task Summary_endpoint_requires_permission_and_returns_the_contract()
    {
        var procedures = new Procedures();
        var context = CreateContext(
            new EvaluacionIssPostgradoCategoria { IdCategoria = 20, Descripcion = "Categoría veinte", IdEvaluacion = 1 });
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                services.AddSingleton<ISadContext>(context);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync("/api/reports/postgraduate/iss-statistics/summary?period=2024A&courseSection=ENECO775%2F1")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE"));
        using var response = await client.GetAsync(
            "/api/reports/postgraduate/iss-statistics/summary?period=2024A&courseSection=ENECO775%2F1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("ENECO775", data.GetProperty("sample").GetProperty("courseCode").GetString());
        Assert.Equal(20, data.GetProperty("categories")[0].GetProperty("id").GetInt32());
        Assert.Equal(5, data.GetProperty("categories")[0].GetProperty("questions")[0]
            .GetProperty("alternative5").GetInt32());
        Assert.Equal("Observación uno", data.GetProperty("observations")[0].GetString());
    }

    private static SadContext CreateContext(params EvaluacionIssPostgradoCategoria[] categories)
    {
        var options = new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("iss-statistics-" + Guid.NewGuid()).Options;
        var context = new SadContext(options);
        context.EvaluacionIssPostgradoCategorias.AddRange(categories);
        context.SaveChanges();
        return context;
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("iss-procedures-" + Guid.NewGuid()).Options))
    {
        public string? SectionPeriod { get; private set; }
        public List<int> StatisticsCalls { get; } = [];
        public bool NoData { get; set; }

        public override Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("20040", 0, "NO", "POSTGRADO"), (limite, modo, conTodos, tipo));
            return Task.FromResult<List<SpRecPeriodosAcademicosProgramacionResult>?>([
                new() { Periodo = "Todos", PeriodoLargo = "Todos" },
                new() { Periodo = "2024A", PeriodoLargo = "2024 - Primer Semestre" },
                new() { Periodo = "2023B", PeriodoLargo = "2023 - Segundo Semestre" },
            ]);
        }

        public override Task<List<SpRecEncuestaIssCatedraSeccionResult>?> SpRec_EncuestaISSCatedraSeccionAsync(
            string? periodo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            SectionPeriod = periodo;
            return Task.FromResult<List<SpRecEncuestaIssCatedraSeccionResult>?>([
                new() { CatedraSeccion = "ENECO775/1", Descripcion = "Economía / ENECO775 / 1" },
                new() { CatedraSeccion = "ENFIN716/2", Descripcion = "Finanzas / ENFIN716 / 2" },
            ]);
        }

        public override Task<List<SpRecEncuestaIssEstadisticasMuestraResult>?> SpRec_EncuestaISSEstadisticasMuestraAsync(
            string? periodo, string? catedraSeccion, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            if (NoData) return Task.FromResult<List<SpRecEncuestaIssEstadisticasMuestraResult>?>([]);
            return Task.FromResult<List<SpRecEncuestaIssEstadisticasMuestraResult>?>([
                new() { Cod_Catedra = "ENECO775", Cod_Seccion = "1", Periodo = "2024A",
                    Inscritos = 10, Contestadas = 8, Porcentaje = 80, Faltantes = 2 },
            ]);
        }

        public override Task<List<SpRecEncuestaIssCategoriasResult>?> SpRec_EncuestaISSCategoriasAsync(
            string? periodo, string? catedraSeccion, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            if (NoData) return Task.FromResult<List<SpRecEncuestaIssCategoriasResult>?>([]);
            return Task.FromResult<List<SpRecEncuestaIssCategoriasResult>?>([
                new() { Id_Categoria = 20 }, new() { Id_Categoria = 10 },
            ]);
        }

        public override Task<List<SpRecEvaluacionIssEstadisticasResult>?> SpRec_EvaluacionISSEstadisticasAsync(
            string? periodo, string? catedra, string? seccion, int? categoria,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("2024A", "ENECO775", "1"), (periodo, catedra, seccion));
            StatisticsCalls.Add(categoria!.Value);
            var name = categoria == 20 ? "Pregunta veinte" : "Pregunta diez";
            return Task.FromResult<List<SpRecEvaluacionIssEstadisticasResult>?>([
                new() { Id_Pregunta = categoria.Value, Pregunta_descripcion = name,
                    Alternativa1 = 1, Alternativa2 = 2, Alternativa3 = 3, Alternativa4 = 4,
                    Alternativa5 = 5, Promedio = 3.5, Total = 15 },
            ]);
        }

        public override Task<List<SpRecEvaluacionIssObservacionesResult>?> SpRec_EvaluacionISSObservacionesAsync(
            string? periodo, string? catedra, string? seccion,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (NoData) return Task.FromResult<List<SpRecEvaluacionIssObservacionesResult>?>([]);
            return Task.FromResult<List<SpRecEvaluacionIssObservacionesResult>?>([
                new() { Observacion = "Observación uno" }, new() { Observacion = "Observación dos" },
            ]);
        }
    }
}
