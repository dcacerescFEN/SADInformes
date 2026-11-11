using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationFilters;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationProcesses;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationResults;
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

public sealed class PermutationResultsTests
{
    [Fact]
    public async Task Query_maps_the_eight_legacy_columns()
    {
        var response = await new GetPermutationResultsHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetPermutationResults("2024A", "2024-01-01T08:00", "2024-01-05T18:00"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "Ana Pérez", "IC", "ENECO660", "Electivo de mención", "01", "02", "ACEPTADA"),
            (row.StudentCode, row.Name, row.StudentType, row.CourseCode, row.CourseName,
                row.FromSection, row.ToSection, row.Result));
        Assert.Equal(("2024A", 1), (response.Data.Period, response.Data.Total));
    }

    [Fact]
    public async Task Endpoints_require_report_access_and_return_filters_processes_and_rows()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync(FiltersUrl)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        var filters = await client.GetFromJsonAsync<ApiResponse<PermutationFiltersDTO>>(FiltersUrl);
        Assert.Equal("2024A", Assert.Single(filters!.Data.Periods).Value);

        var processes = await client.GetFromJsonAsync<ApiResponse<PermutationProcessesDTO>>(ProcessesUrl);
        Assert.Equal("PERMUTA", Assert.Single(processes!.Data.Processes).Text);

        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<PermutationResultsReportDTO>>();
        Assert.Equal("A/1", Assert.Single(body!.Data.Rows).StudentCode);
    }

    [Fact]
    public async Task Invalid_period_is_rejected_without_querying_the_report()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(
            "/api/reports/academic-enrollment/permutation-results-by-student/report?period=&from=2024-01-01&to=2024-01-02");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/permutation-results-by-student/filters";
    private const string ProcessesUrl =
        "/api/reports/academic-enrollment/permutation-results-by-student/processes?period=2024A";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/permutation-results-by-student/report?period=2024A&from=2024-01-01T08:00&to=2024-01-05T18:00";

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
            .UseInMemoryDatabase("permutation-results-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecPeriodosActivosTomaRamosResult>?> SpRec_PeriodosActivosTomaRamosAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosActivosTomaRamosResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", PeriodoExtraordinario = false },
            ]);

        public override Task<List<SpRecProcesosPeriodosResult>?> SpRec_ProcesosPeriodosAsync(
            string? tipoProceso, string? periodo, int? idProcesoPeriodo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecProcesosPeriodosResult>?>([
                new() { IdProcesoPeriodo = 1, Nombre = "PERMUTA",
                    FechaInicio = new DateTime(2024, 1, 1, 8, 0, 0), FechaTermino = new DateTime(2024, 1, 5, 18, 0, 0) },
            ]);

        public override Task<List<SpRecResPermutaXAlumnoResult>?> SpRec_ResPermutaXAlumnoAsync(
            string? periodo, DateTime? desde, DateTime? hasta,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecResPermutaXAlumnoResult>?>([
                new() { Cod_Alumno = "A/1", Nombre = "Ana Pérez", Tipo = "IC", Cod_Catedra = "ENECO660",
                    Nom_Catedra = "Electivo de mención", Desde = "01", Hacia = "02", Estado = "ACEPTADA" },
            ]);
    }
}
