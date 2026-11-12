using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationFilters;
using Application.Reports.AcademicEnrollment.RoomAssignmentCourses.Queries.GetRoomAssignmentCourses;
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

public sealed class RoomAssignmentCoursesTests
{
    [Fact]
    public async Task Query_maps_the_eighteen_legacy_columns()
    {
        var response = await new GetRoomAssignmentCoursesHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetRoomAssignmentCourses("2024A"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(
            ("2024A", "ENECO660", "01", "FALSO", "Lun 10:00", "CATEDRA", "20", "15",
                "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10"),
            (row.Period, row.CourseCode, row.Section, row.Deleted, row.Classes, row.Type,
                row.OfferedVacancies, row.OccupiedVacancies,
                row.Teacher1, row.Teacher2, row.Teacher3, row.Teacher4, row.Teacher5,
                row.Teacher6, row.Teacher7, row.Teacher8, row.Teacher9, row.Teacher10));
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
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<RoomAssignmentCoursesReportDTO>>();
        Assert.Equal("P10", Assert.Single(body!.Data.Rows).Teacher10);
    }

    [Fact]
    public async Task Export_returns_xlsx_with_the_eighteen_headers()
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
        Assert.Contains("CursosAsignacionSalas2024A.xlsx",
            response.Content.Headers.ContentDisposition?.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var archive = new System.IO.Compression.ZipArchive(new System.IO.MemoryStream(bytes));
        using var sheet = new System.IO.StreamReader(
            archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = sheet.ReadToEnd();
        foreach (var header in new[]
        {
            "Período", "Cátedra", "Sección", "Eliminado", "Clases", "Tipo",
            "Vacantes Ofrecidas", "Vacantes Ocupadas",
            "Profesor 1", "Profesor 10",
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
            "/api/reports/academic-enrollment/courses-for-room-assignment/report?period=");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/courses-for-room-assignment/filters";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/courses-for-room-assignment/report?period=2024A";
    private const string ExportUrl =
        "/api/reports/academic-enrollment/courses-for-room-assignment/export?period=2024A";

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
            .UseInMemoryDatabase("room-assignment-" + Guid.NewGuid()).Options))
    {
        public bool FailOnReport { get; init; }

        public override Task<List<SpRecPeriodosActivosTomaRamosResult>?> SpRec_PeriodosActivosTomaRamosAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosActivosTomaRamosResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", PeriodoExtraordinario = false },
            ]);

        public override Task<List<SpRecCursosSistemaAsignacionSalasResult>?>
            SpRec_CursosSistemaAsignacionSalasAsync(
                string? periodo,
                OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (FailOnReport) throw new InvalidOperationException("El informe no debe consultarse.");
            return Task.FromResult<List<SpRecCursosSistemaAsignacionSalasResult>?>([
                new()
                {
                    Periodo = "2024A", Cod_Catedra = "ENECO660", Cod_Seccion = "01",
                    Eliminado = "FALSO", Clases = "Lun 10:00", Tipo = "CATEDRA",
                    VacantesOfrecidas = 20, VacantesOcupadas = 15,
                    Profesor1 = "P1", Profesor2 = "P2", Profesor3 = "P3", Profesor4 = "P4",
                    Profesor5 = "P5", Profesor6 = "P6", Profesor7 = "P7", Profesor8 = "P8",
                    Profesor9 = "P9", Profesor10 = "P10",
                },
            ]);
        }
    }
}
