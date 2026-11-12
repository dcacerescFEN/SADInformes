using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Queries.GetTeachingScheduleDecreto;
using Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Queries.GetTeachingScheduleDecretoFilters;
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

public sealed class TeachingScheduleDecretoTests
{
    [Fact]
    public async Task Query_maps_the_nine_legacy_columns_and_plan_title()
    {
        var response = await new GetTeachingScheduleDecretoHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetTeachingScheduleDecreto("teaching-schedule-ic-old-plan", "2024A"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(
            ("ENECO660/01", "ENECO660", "ELECTIVO", "Obligatoria", "4", "PC",
                "Sin requisitos", "ANA PÉREZ", "Lun 10:00"),
            (row.Course, row.CourseCode, row.CourseName, row.Type, row.Credits, row.Mention,
                row.Requirements, row.Teachers, row.Schedule));
        Assert.Equal(("Ingeniería Comercial Plan Antiguo", "2024A", 1),
            (response.Data.Title, response.Data.Period, response.Data.Total));
    }

    [Fact]
    public async Task Endpoints_require_report_access_and_return_periods_and_rows()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync(FiltersUrl)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        var filters = await client.GetFromJsonAsync<ApiResponse<TeachingScheduleDecretoFiltersDTO>>(FiltersUrl);
        Assert.Equal("Ingeniería Comercial Plan Antiguo", filters!.Data.Title);
        Assert.Equal("2024A", Assert.Single(filters.Data.Periods).Value);

        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<TeachingScheduleDecretoReportDTO>>();
        Assert.Equal("ENECO660", Assert.Single(body!.Data.Rows).CourseCode);
    }

    [Theory]
    [InlineData("teaching-schedule-unknown", "2024A")]
    [InlineData("teaching-schedule-ic-old-plan", "")]
    public async Task Invalid_slug_or_period_is_rejected_without_querying_the_report(
        string slug, string period)
    {
        var procedures = new Procedures { FailOnReport = true };
        var tokens = new TokenFactory();
        using var factory = CreateFactory(procedures, tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(
            $"/api/reports/academic-enrollment/teaching-schedule-decreto/report?slug={slug}&period={period}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/teaching-schedule-decreto/filters?slug=teaching-schedule-ic-old-plan";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/teaching-schedule-decreto/report?slug=teaching-schedule-ic-old-plan&period=2024A";

    private static WebApplicationFactory<Program> CreateFactory(Procedures procedures, TokenFactory tokens) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("teaching-schedule-decreto-" + Guid.NewGuid()).Options))
    {
        public bool FailOnReport { get; init; }

        public override Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosAcademicosProgramacionResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", Inscripcion = "FALSE" },
            ]);

        public override Task<List<SpRecProgramacionDocenteXDecretoResult>?> SpRec_ProgramacionDocenteXDecretoAsync(
            string? periodo, string? decreto, string? tipoPeriodo, string? escuela,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (FailOnReport) throw new InvalidOperationException("El informe no debe consultarse.");
            return Task.FromResult<List<SpRecProgramacionDocenteXDecretoResult>?>([
                new()
                {
                    Curso = "ENECO660/01", Cod_CatedraHija = "ENECO660", Nom_CatedraHija = "ELECTIVO",
                    Tipo = "Obligatoria", Ud = 4, Mencion = "PC", Requisitos = "Sin requisitos",
                    Profesores = "ANA PÉREZ", Horario = "Lun 10:00",
                },
            ]);
        }
    }
}
