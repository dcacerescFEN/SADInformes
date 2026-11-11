using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationFilters;
using Application.Reports.AcademicEnrollment.StudentsCompletedElectives.Queries.GetStudentsCompletedElectives;
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

public sealed class StudentsCompletedElectivesTests
{
    [Fact]
    public async Task Query_maps_the_six_legacy_columns()
    {
        var response = await new GetStudentsCompletedElectivesHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetStudentsCompletedElectives("2024A"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "Ana Pérez", "ENECO660/01", "Electivo", "009615/2003", "Electivo"),
            (row.StudentCode, row.Name, row.Course, row.CourseName, row.Decree, row.CourseType));
        Assert.Equal(("2024A", 1), (response.Data.Period, response.Data.Total));
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
        var filters = await client.GetFromJsonAsync<ApiResponse<PermutationFiltersDTO>>(FiltersUrl);
        Assert.Equal("2024A", Assert.Single(filters!.Data.Periods).Value);

        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<StudentsCompletedElectivesReportDTO>>();
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
            "/api/reports/academic-enrollment/students-completed-electives/report?period=");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/students-completed-electives/filters";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/students-completed-electives/report?period=2024A";

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
            .UseInMemoryDatabase("students-completed-electives-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecPeriodosActivosTomaRamosResult>?> SpRec_PeriodosActivosTomaRamosAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosActivosTomaRamosResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", PeriodoExtraordinario = false },
            ]);

        public override Task<List<SpRecAlumnosCompletaronElectivosResult>?> SpRec_AlumnosCompletaronElectivosAsync(
            string? periodo, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAlumnosCompletaronElectivosResult>?>([
                new() { Cod_Alumno = "A/1", Nombre = "Ana Pérez", Curso = "ENECO660/01",
                    Nom_Catedra = "Electivo", Decreto_Alumno = "009615/2003", TipoCatedra = "Electivo" },
            ]);
    }
}
