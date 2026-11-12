using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.CourseCrossing.Queries.GetCourseCrossing;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Reporting;
using Xunit;

namespace WebApi.Tests;

public sealed class CourseCrossingTests
{
    [Fact]
    public async Task Query_maps_the_eight_legacy_columns()
    {
        var response = await new GetCourseCrossingHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetCourseCrossing("2024A"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(
            ("ENECO660/01", "ELECTIVO", "ENECO661/02", "OTRO ELECTIVO", "3", "4", "5", "12"),
            (row.Course1, row.CourseName1, row.Course2, row.CourseName2,
                row.IcLimit, row.IicgLimit, row.OtherLimit, row.TotalLimit));
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
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<CourseCrossingReportDTO>>();
        Assert.Equal("12", Assert.Single(body!.Data.Rows).TotalLimit);
    }

    [Fact]
    public async Task Export_returns_xlsx_with_the_crossing_headers()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(ExportUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("CruceCursos.xlsx", response.Content.Headers.ContentDisposition?.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var archive = new System.IO.Compression.ZipArchive(new System.IO.MemoryStream(bytes));
        using var sheet = new System.IO.StreamReader(
            archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = sheet.ReadToEnd();
        foreach (var header in new[]
        {
            "Curso 1", "Cátedra 1", "Curso 2", "Cátedra 2",
            "Topes IC", "Topes IICG", "Topes Otros", "Topes Total",
        })
            Assert.Contains(header, xml);
    }

    [Fact]
    public async Task Invalid_period_is_rejected_without_querying_the_report()
    {
        var procedures = new Procedures { FailOnReport = true };
        var tokens = new TokenFactory();
        using var factory = CreateFactory(procedures, tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(
            "/api/reports/academic-enrollment/course-crossing/report?period=");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/course-crossing/filters";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/course-crossing/report?period=2024A";
    private const string ExportUrl =
        "/api/reports/academic-enrollment/course-crossing/export?period=2024A";

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
            .UseInMemoryDatabase("course-crossing-" + Guid.NewGuid()).Options))
    {
        public bool FailOnReport { get; init; }

        public override Task<List<SpRecPeriodosActivosTomaRamosResult>?> SpRec_PeriodosActivosTomaRamosAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosActivosTomaRamosResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", PeriodoExtraordinario = false },
            ]);

        public override Task<List<SpRecCruceCursosResult>?> SpRec_CruceCursosAsync(
            string? periodo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (FailOnReport) throw new InvalidOperationException("El informe no debe consultarse.");
            return Task.FromResult<List<SpRecCruceCursosResult>?>([
                new()
                {
                    Curso1 = "ENECO660/01", Catedra1 = "ELECTIVO", Curso2 = "ENECO661/02",
                    Catedra2 = "OTRO ELECTIVO", TopesIC = 3, TopesIICG = 4, TopesOtros = 5,
                    TopesTotal = 12,
                },
            ]);
        }
    }
}
