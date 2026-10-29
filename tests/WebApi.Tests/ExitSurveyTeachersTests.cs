using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsInsatisfaction;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsTeacherDetail;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsTeachers;
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

public sealed class ExitSurveyTeachersTests
{
    private const string Route = "/api/reports/postgraduate/exit-survey-statistics";
    private const string Query = "?period=2024A&studentType=MBA&modality=PT";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_STATISTICS";

    [Fact]
    public async Task Teachers_map_I_theoretical_II_practical_and_III_formative()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsTeachersHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsTeachers("2024A", "MBA", "PT"), default);
        Assert.Equal(new[] { "Teórico" }, result.Data.Theoretical.Select(row => row.Professor));
        Assert.Equal(new[] { "Práctico" }, result.Data.Practical.Select(row => row.Professor));
        Assert.Equal(new[] { "Formativo" }, result.Data.Formative.Select(row => row.Professor));
        Assert.Equal((5, 4, 3), (result.Data.Theoretical[0].Frequency, result.Data.Practical[0].Frequency, result.Data.Formative[0].Frequency));
        Assert.Equal(("2024A", "MBA", "PT"), (procedures.Period, procedures.Type, procedures.Modality));
        Assert.Equal(new[] { "I", "II", "III" }, procedures.Calls);
    }

    [Fact]
    public async Task Insatisfaction_maps_drilldown_identity_and_count()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsInsatisfactionHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsInsatisfaction("2024A", "MBA", "PT"), default);
        var row = Assert.Single(result.Data.Rows);
        Assert.Equal(("Profesor Insatisfecho", 3, "P9", "2024A", "MBA", "PT"),
            (row.Professor, row.Count, row.ProfessorCode, row.Period, row.StudentType, row.Modality));
    }

    [Fact]
    public async Task Detail_requires_a_professor_and_passes_all_four_filters()
    {
        var procedures = new Procedures();
        var result = await new GetExitSurveyStatisticsTeacherDetailHandler(procedures, new ApiResponseService())
            .Handle(new GetExitSurveyStatisticsTeacherDetail("2024A", "MBA", "PT", "P9"), default);
        Assert.Equal(("2024A", "MBA", "PT", "P9"), (procedures.Period, procedures.Type, procedures.Modality, procedures.Professor));
        Assert.Equal(new[] { "Motivo uno", "Motivo dos" }, result.Data.Motives);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123456789012345678901")]
    public async Task Detail_rejects_invalid_professor_without_calling_procedure(string? professor)
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetExitSurveyStatisticsTeacherDetailHandler(procedures, new ApiResponseService())
                .Handle(new GetExitSurveyStatisticsTeacherDetail("2024A", "MBA", "PT", professor), default));
        Assert.Null(procedures.Professor);
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
    public async Task All_teacher_and_detail_routes_require_the_statistics_permission(string? role, int status)
    {
        var (factory, tokens) = Create(new Procedures()); using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/teachers" + Query, "/insatisfaction" + Query, "/insatisfaction/detail" + Query + "&professor=P9" })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
    }

    [Fact]
    public async Task Authorized_routes_return_corrected_teacher_roles_and_detail_with_no_store()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures); using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var teachers = await client.GetAsync(Route + "/teachers" + Query); Assert.Equal(HttpStatusCode.OK, teachers.StatusCode); Assert.True(teachers.Headers.CacheControl!.NoStore);
        using var teachersJson = JsonDocument.Parse(await teachers.Content.ReadAsStringAsync());
        var data = teachersJson.RootElement.GetProperty("data");
        Assert.Equal("Teórico", data.GetProperty("theoretical")[0].GetProperty("professor").GetString());
        Assert.Equal("Práctico", data.GetProperty("practical")[0].GetProperty("professor").GetString());
        Assert.Equal("Formativo", data.GetProperty("formative")[0].GetProperty("professor").GetString());
        using var insatisfaction = await client.GetAsync(Route + "/insatisfaction" + Query); Assert.Equal(HttpStatusCode.OK, insatisfaction.StatusCode);
        using var detail = await client.GetAsync(Route + "/insatisfaction/detail" + Query + "&professor=P9"); Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("Motivo uno", await detail.Content.ReadAsStringAsync());
    }

    private sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options))
    {
        public string? Period { get; private set; }
        public string? Type { get; private set; }
        public string? Modality { get; private set; }
        public string? Professor { get; private set; }
        public List<string> Calls { get; } = [];
        private Task<List<SpRecEncuestaGraduadosProfesoresResult>?> Teacher(string role, string period, string? type, string? modality)
        {
            Calls.Add(role); Period = period; Type = type; Modality = modality;
            var frequency = role == "I" ? 5 : role == "II" ? 4 : 3;
            return Task.FromResult<List<SpRecEncuestaGraduadosProfesoresResult>?>([new() { Profesor = role == "I" ? "Teórico" : role == "II" ? "Práctico" : "Formativo", Frecuencia = frequency }]);
        }
        public override Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIAsync(string? p, string? t, string? m, OutputParameter<int>? r = null, CancellationToken? c = default) => Teacher("I", p!, t, m);
        public override Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIIAsync(string? p, string? t, string? m, OutputParameter<int>? r = null, CancellationToken? c = default) => Teacher("II", p!, t, m);
        public override Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIIIAsync(string? p, string? t, string? m, OutputParameter<int>? r = null, CancellationToken? c = default) => Teacher("III", p!, t, m);
        public override Task<List<SpRecEncuestaGraduadosInsatisfaccionResult>?> spRec_EncuestaGraduadosEstadisticasInsatisfaccionAsync(string? p, string? t, string? m, OutputParameter<int>? r = null, CancellationToken? c = default)
        {
            Period = p; Type = t; Modality = m;
            return Task.FromResult<List<SpRecEncuestaGraduadosInsatisfaccionResult>?>([new() { Profesor = "Profesor Insatisfecho", Cantidad = 3, CodPersona = "P9", Periodo = p, TipoAlumno = t, Modalidad = m }]);
        }
        public override Task<List<SpRecEncuestaGraduadosDetalleProfResult>?> spRec_EncuestaGraduadosEstadisticasDetalleProfAsync(string? p, string? t, string? m, string? professor, OutputParameter<int>? r = null, CancellationToken? c = default)
        {
            Period = p; Type = t; Modality = m; Professor = professor;
            return Task.FromResult<List<SpRecEncuestaGraduadosDetalleProfResult>?>([new() { Motivos = "Motivo uno" }, new() { Motivos = "Motivo dos" }]);
        }
    }
}
