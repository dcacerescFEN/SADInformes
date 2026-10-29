using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsFilters;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsCatedrasArea;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsLikert;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSkills;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSummary;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSample;
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

public sealed class ExitSurveyStatisticsTests
{
    private const string Route = "/api/reports/postgraduate/exit-survey-statistics";
    private const string Query = "?period=2024A&studentType=Todos&modality=Todos";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_STATISTICS";

    [Fact]
    public async Task Filters_default_to_legacy_all_values_and_return_the_hardcoded_type_list()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsFilters(null), default);
        Assert.Equal(new[] { "2024A", "2023B", "Todos" }, result.Data.Periods.Select(row => row.Value));
        Assert.Equal(new[] { "Todos", "PT" }, result.Data.Modalities.Select(row => row.Value));
        Assert.Equal(13, result.Data.StudentTypes.Length);
        Assert.Equal("Todos", result.Data.StudentTypes[0].Value);
        Assert.Equal("MAE", result.Data.StudentTypes[^1].Value);
        Assert.Equal("Todos", procedures.ModalityType);
    }

    [Fact]
    public async Task Sample_preserves_legacy_totals_and_nulls_become_zero()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsSampleHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsSample("2024A", "MBA", "PT"), default);
        Assert.Equal(("2024A", "MBA", "PT"), (procedures.Period, procedures.Type, procedures.Modality));
        Assert.Equal(2, result.Data.Rows.Length);
        Assert.Equal(new ExitSurveySampleRowDTO("MBA", "PT", 10, 2, 8), result.Data.Rows[0]);
        Assert.Equal(new ExitSurveySampleRowDTO("MBA GLOBAL", null, 0, 0, 0), result.Data.Rows[1]);
        Assert.Equal(new ExitSurveySampleTotalsDTO(10, 2, 8), result.Data.Totals);
    }

    [Theory]
    [InlineData("Todos", "Todos", "Todos")]
    [InlineData(null, null, null)]
    public async Task Empty_filters_are_sent_as_Todos_and_Todos_type_is_valid(string? period, string? type, string? modality)
    {
        var procedures = new Procedures();
        await new GetExitSurveyStatisticsSampleHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsSample(period, type, modality), default);
        Assert.Equal(("Todos", "Todos", "Todos"), (procedures.Period, procedures.Type, procedures.Modality));
    }

    [Theory]
    [InlineData("2024AA", "Todos", "Todos", "El período no es válido.")]
    [InlineData("2024A", "UNKNOWN", "Todos", "El tipo de alumno no es válido.")]
    [InlineData("2024A", "Todos", "DEMASIADO_LARGA", "La modalidad no es válida.")]
    public async Task Invalid_filter_values_fail_before_the_sample_query(string period, string type, string modality, string message)
    {
        var procedures = new Procedures();
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetExitSurveyStatisticsSampleHandler(procedures, new ApiResponseService())
                .Handle(new GetExitSurveyStatisticsSample(period, type, modality), default));
        Assert.Contains(message, error.Message); Assert.Null(procedures.Period);
    }

    [Theory]
    [InlineData("malla", "Q1", "Malla Curricular")]
    [InlineData("cuerpo-academico", "Q2", "Cuerpo Académico")]
    [InlineData("funcionamiento", "Q3", "Funcionamiento")]
    [InlineData("infraestructura", "Q4", "Infraestructura y recursos")]
    [InlineData("servicios", "Q5", "Servicios")]
    [InlineData("vinculacion-medio", "Q6", "Vinculación Medio")]
    [InlineData("networking", "Q7", "Networking")]
    public async Task Likert_maps_each_section_to_its_canonical_question_and_totals_alternatives(string section, string question, string title)
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsLikertHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsLikert(section, "2024A", "MBA", "PT"), default);
        Assert.Equal((question, title), (procedures.LikertCalls.Single(), result.Data.Title));
        Assert.Equal(("2024A", "MBA", "PT"), (procedures.Period, procedures.Type, procedures.Modality));
        Assert.Equal(2, result.Data.Rows.Length);
        Assert.Equal(3, result.Data.Rows[0].Alternative1);
        Assert.Equal(4.5, result.Data.Rows[0].Average);
        Assert.Equal(new ExitSurveyLikertTotalsDTO(5, 7, 9, 11, 13), result.Data.Totals);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Q1")]
    [InlineData("MALLA")]
    public async Task Invalid_likert_section_never_calls_procedures(string? section)
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetExitSurveyStatisticsLikertHandler(procedures, new ApiResponseService())
                .Handle(new GetExitSurveyStatisticsLikert(section, "2024A", "MBA", "PT"), default));
        Assert.Empty(procedures.LikertCalls);
    }

    [Fact]
    public async Task Likert_keeps_empty_result_and_does_not_invent_totals()
    {
        var result = await new GetExitSurveyStatisticsLikertHandler(new Procedures { EmptyLikert = true }, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsLikert("malla", "2024A", "MBA", "PT"), default);
        Assert.Empty(result.Data.Rows);
        Assert.Equal(new ExitSurveyLikertTotalsDTO(0, 0, 0, 0, 0), result.Data.Totals);
    }

    [Fact]
    public async Task Q8_preserves_five_alternatives_codes_and_totals()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsCatedrasAreaHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsCatedrasArea("2024A", "MBA", "PT"), default);
        Assert.Equal(("2024A", "MBA", "PT"), (procedures.Period, procedures.Type, procedures.Modality));
        var row = Assert.Single(result.Data.Rows);
        Assert.Equal(("C1", "Cátedra Uno", 1, 2, 3, 4, 5, 3.0, 15),
            (row.Code, row.Description, row.Alternative1, row.Alternative2, row.Alternative3, row.Alternative4, row.Alternative5, row.Average, row.Total));
        Assert.Equal(new ExitSurveyCatedraTotalsDTO(1, 2, 3, 4, 5), result.Data.Totals);
    }

    [Fact]
    public async Task Q11_preserves_seven_alternatives_and_totals()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsSkillsHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsSkills("Todos", "MGP", "Todos"), default);
        var row = Assert.Single(result.Data.Rows);
        Assert.Equal(("Q11", "Habilidad", 1, 2, 3, 4, 5, 6, 7, 4.0, 28),
            (row.Question, row.Description, row.Alternative1, row.Alternative2, row.Alternative3, row.Alternative4, row.Alternative5, row.Alternative6, row.Alternative7, row.Average, row.Total));
        Assert.Equal(new ExitSurveySkillTotalsDTO(1, 2, 3, 4, 5, 6, 7), result.Data.Totals);
    }

    [Fact]
    public async Task Summary_computes_footer_percentages_and_weighted_dimension_for_seven_sections()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsSummaryHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsSummary("2024A", "MBA", "PT"), default);
        Assert.Equal(7, result.Data.Rows.Length);
        Assert.Equal(new[] { "Malla Curricular", "Cuerpo Académico", "Funcionamiento", "Infraestructura y recursos", "Servicios", "Vinculación Medio", "Networking" }, result.Data.Rows.Select(row => row.Title));
        var row = result.Data.Rows[0];
        Assert.Equal((5, 7, 9, 11, 13, 45), (row.Alternative1, row.Alternative2, row.Alternative3, row.Alternative4, row.Alternative5, row.Total));
        Assert.Equal(155d / 45d, row.Dimension!.Value, 10);
        Assert.Equal(7, procedures.LikertCalls.Count);
    }

    [Fact]
    public async Task Summary_keeps_zero_totals_and_does_not_invent_a_dimension()
    {
        var result = await new GetExitSurveyStatisticsSummaryHandler(new Procedures { EmptyLikert = true }, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsSummary("2024A", "MBA", "PT"), default);
        Assert.All(result.Data.Rows, row => { Assert.Equal(0, row.Total); Assert.Null(row.Dimension); });
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

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES", 403)]
    public async Task Both_routes_require_the_specific_read_permission(string? role, int status)
    {
        var (factory, tokens) = Create(new Procedures()); using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters", "/sample" + Query, "/likert?section=malla&period=2024A&studentType=Todos&modality=Todos", "/catedras-area?period=2024A&studentType=Todos&modality=Todos", "/skills?period=2024A&studentType=Todos&modality=Todos", "/summary?period=2024A&studentType=Todos&modality=Todos" })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
    }

    [Theory]
    [InlineData(View)]
    [InlineData("ROLE_REPORT_ADMIN")]
    [InlineData("ROLE_REPORT_SUPERADMIN")]
    public async Task Authorized_roles_receive_filters_and_sample_with_no_store(string role)
    {
        var (factory, tokens) = Create(new Procedures()); using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", role));
        using var filters = await client.GetAsync(Route + "/filters"); Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.True(filters.Headers.CacheControl!.NoStore); Assert.Contains("MAE", await filters.Content.ReadAsStringAsync());
        using var sample = await client.GetAsync(Route + "/sample" + Query); Assert.Equal(HttpStatusCode.OK, sample.StatusCode);
        Assert.True(sample.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await sample.Content.ReadAsStringAsync());
        Assert.Equal(10, json.RootElement.GetProperty("data").GetProperty("totals").GetProperty("sample").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("data").GetProperty("rows")[0].GetProperty("inProgress").GetInt32());
        using var likert = await client.GetAsync(Route + "/likert?section=malla&period=2024A&studentType=Todos&modality=Todos"); Assert.Equal(HttpStatusCode.OK, likert.StatusCode);
        Assert.True(likert.Headers.CacheControl!.NoStore); Assert.Contains("Malla Curricular", await likert.Content.ReadAsStringAsync());
        using var summary = await client.GetAsync(Route + "/summary?period=2024A&studentType=Todos&modality=Todos"); Assert.Equal(HttpStatusCode.OK, summary.StatusCode); Assert.Contains("dimension", await summary.Content.ReadAsStringAsync());
        using var q8 = await client.GetAsync(Route + "/catedras-area?period=2024A&studentType=Todos&modality=Todos"); Assert.Equal(HttpStatusCode.OK, q8.StatusCode); Assert.Contains("Cátedra Uno", await q8.Content.ReadAsStringAsync());
        using var q11 = await client.GetAsync(Route + "/skills?period=2024A&studentType=Todos&modality=Todos"); Assert.Equal(HttpStatusCode.OK, q11.StatusCode); Assert.Contains("Habilidad", await q11.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Invalid_filters_and_storage_failures_are_sanitized()
    {
        var (factory, tokens) = Create(new Procedures()); using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + "/sample?period=2024AA")).StatusCode);
        var failing = new Procedures { Fail = true }; var (badFactory, badTokens) = Create(failing); using var badHost = badFactory; using var badSigning = badTokens; using var bad = badFactory.CreateClient();
        bad.SetBearer(badTokens.CreateToken("SADInformes", View));
        using var response = await bad.GetAsync(Route + "/sample" + Query);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("El informe no está disponible.", await response.Content.ReadAsStringAsync());
    }

    private sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options))
    {
        public bool Fail { get; init; }
        public string? ModalityType { get; private set; }
        public string? Period { get; private set; }
        public string? Type { get; private set; }
        public string? Modality { get; private set; }
        public bool EmptyLikert { get; init; }
        public List<string> LikertCalls { get; } = [];
        public override Task<List<SpRecEncuestaGraduadosPeriodosResult>?> spRec_EncuestaGraduados_PeriodosAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecEncuestaGraduadosPeriodosResult>?>(
            [new() { Periodo = "2024A", Descripcion = "2024" }, new() { Periodo = "2023B", Descripcion = "2023" }, new() { Periodo = "Todos", Descripcion = "Todos" }]);
        }
        public override Task<List<SpRecEncuestaGraduadosModalidadesResult>?> spRec_EncuestaGraduados_ModalidadesAsync(
            string? tipo, OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            ModalityType = tipo;
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecEncuestaGraduadosModalidadesResult>?>([new() { Modalidad = "Todos" }, new() { Modalidad = "PT" }]);
        }
        private Task<List<SpRecEncuestaGraduadosLikertResult>?> Likert(string question)
        {
            LikertCalls.Add(question);
            return Task.FromResult<List<SpRecEncuestaGraduadosLikertResult>?>(EmptyLikert ? [] :
            [
                new() { Pregunta = "1", Descripcion = "Pregunta A", Alternativa1 = 3, Alternativa2 = 4, Alternativa3 = 5, Alternativa4 = 6, Alternativa5 = 7, Promedio = 4.5, Total = 25 },
                new() { Pregunta = "2", Descripcion = "Pregunta B", Alternativa1 = 2, Alternativa2 = 3, Alternativa3 = 4, Alternativa4 = 5, Alternativa5 = 6, Promedio = 4.0, Total = 20 },
            ]);
        }
        public override Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ1Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default) => RecordLikert("Q1", periodo, tipo_Alumno, modalidad);
        public override Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ2Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default) => RecordLikert("Q2", periodo, tipo_Alumno, modalidad);
        public override Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ3Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default) => RecordLikert("Q3", periodo, tipo_Alumno, modalidad);
        public override Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ4Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default) => RecordLikert("Q4", periodo, tipo_Alumno, modalidad);
        public override Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ5Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default) => RecordLikert("Q5", periodo, tipo_Alumno, modalidad);
        public override Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ6Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default) => RecordLikert("Q6", periodo, tipo_Alumno, modalidad);
        public override Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ7Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default) => RecordLikert("Q7", periodo, tipo_Alumno, modalidad);
        private Task<List<SpRecEncuestaGraduadosLikertResult>?> RecordLikert(string question, string? period, string? type, string? modality)
        {
            Period = period; Type = type; Modality = modality;
            return Likert(question);
        }
        public override Task<List<SpRecEncuestaGraduadosQ8Result>?> spRec_EncuestaGraduadosEstadisticasQ8Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default)
        {
            Period = periodo; Type = tipo_Alumno; Modality = modalidad;
            return Task.FromResult<List<SpRecEncuestaGraduadosQ8Result>?>([new() { CodCatedra = "C1", Descripcion = "Cátedra Uno", Alternativa1 = 1, Alternativa2 = 2, Alternativa3 = 3, Alternativa4 = 4, Alternativa5 = 5, Promedio = 3.0, Total = 15 }]);
        }
        public override Task<List<SpRecEncuestaGraduadosQ11Result>?> spRec_EncuestaGraduadosEstadisticasQ11Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? r = null, CancellationToken? ct = default)
        {
            Period = periodo; Type = tipo_Alumno; Modality = modalidad;
            return Task.FromResult<List<SpRecEncuestaGraduadosQ11Result>?>([new() { Pregunta = "Q11", Descripcion = "Habilidad", Alternativa1 = 1, Alternativa2 = 2, Alternativa3 = 3, Alternativa4 = 4, Alternativa5 = 5, Alternativa6 = 6, Alternativa7 = 7, Promedio = 4.0, Total = 28 }]);
        }
        public override Task<List<SpRecEncuestaGraduadosEstadisticasResult>?> spRec_EncuestaGraduados_EstadisticasAsync(
            string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            Period = periodo; Type = tipo_Alumno; Modality = modalidad;
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecEncuestaGraduadosEstadisticasResult>?>(
            [new() { Tipo = "MBA", Modalidad = "PT", Muestra = 10, EnProceso = 2, Contestadas = 8 }, new() { Tipo = "MBA GLOBAL", Modalidad = null, Muestra = null, EnProceso = null, Contestadas = null }]);
        }
    }
}
