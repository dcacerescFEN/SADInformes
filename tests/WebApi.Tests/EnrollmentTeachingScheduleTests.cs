using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingSchedule;
using Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingScheduleFilters;
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

public sealed class EnrollmentTeachingScheduleTests
{
    [Fact]
    public async Task Query_maps_the_eleven_legacy_columns()
    {
        var response = await new GetEnrollmentTeachingScheduleHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetEnrollmentTeachingSchedule("POSTGRADO", "2024A", "%"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(
            ("ENECO660/01", "ELECTIVO", "ANA PÉREZ", "Lun 10:00", "Mar 12:00",
                "Sala 1", "Sala 2", "20", "15", "Economía", "12.345.678-9"),
            (row.Course, row.CourseName, row.Teachers, row.Classes, row.Assistantships,
                row.Room, row.AssistantRoom, row.Vacancies, row.Enrolled, row.Area, row.TeacherId));
        Assert.Equal(("2024A", "POSTGRADO", "%", 1),
            (response.Data.Period, response.Data.School, response.Data.Area, response.Data.Total));
    }

    [Fact]
    public async Task Endpoints_require_report_access_and_return_filters_and_rows()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync(FiltersUrl)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        var filters = await client.GetFromJsonAsync<ApiResponse<EnrollmentTeachingScheduleFiltersDTO>>(FiltersUrl);
        Assert.Equal("2024A", Assert.Single(filters!.Data.Periods).Value);
        Assert.Equal("%", Assert.Single(filters.Data.Areas).Value);

        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<EnrollmentTeachingScheduleReportDTO>>();
        Assert.Equal("12.345.678-9", Assert.Single(body!.Data.Rows).TeacherId);
    }

    [Fact]
    public async Task Export_returns_the_same_filtered_rows_as_xlsx()
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
        Assert.Contains("ProgramacionDocente.xlsx", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("PK", await ReadSignature(response));
    }

    [Theory]
    [InlineData("escuela=OTRA", "2024A", "%")]
    [InlineData("escuela=PREGRADO", "", "%")]
    [InlineData("escuela=PREGRADO", "2024A", "demasiado-largo-para-el-area-del-informe-que-supera-el-limite-de-cien-caracteres")]
    public async Task Invalid_filters_are_rejected_without_querying_the_report(
        string school, string period, string area)
    {
        var procedures = new Procedures { FailOnReport = true };
        var tokens = new TokenFactory();
        using var factory = CreateFactory(procedures, tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(
            $"/api/reports/academic-enrollment/enrollment-teaching-schedule/report?{school}&period={period}&area={area}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Excel_contains_the_eleven_headers()
    {
        var file = EnrollmentTeachingScheduleExcel.Create(
        [
            new("ENECO660/01", "ELECTIVO", "ANA PÉREZ", "Lun 10:00", "Mar 12:00", "Sala 1",
                "Sala 2", "20", "15", "Economía", "12.345.678-9"),
        ]);

        using var archive = new System.IO.Compression.ZipArchive(new System.IO.MemoryStream(file));
        using var sheet = new System.IO.StreamReader(
            archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = sheet.ReadToEnd();
        foreach (var header in new[]
        {
            "Curso", "Cátedra", "Profesor(es)", "Clases", "Ayudantías", "Sala",
            "Sala Ayudantías", "Vacantes", "Inscritos", "Área", "Rut Profesor",
        })
            Assert.Contains(header, xml);
    }

    private static async Task<string> ReadSignature(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[2];
        Assert.Equal(2, await stream.ReadAsync(buffer));
        return System.Text.Encoding.ASCII.GetString(buffer);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/enrollment-teaching-schedule/filters?school=PREGRADO";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/enrollment-teaching-schedule/report?school=PREGRADO&period=2024A&area=%25";
    private const string ExportUrl =
        "/api/reports/academic-enrollment/enrollment-teaching-schedule/export?school=PREGRADO&period=2024A&area=%25";

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
            .UseInMemoryDatabase("enrollment-teaching-schedule-" + Guid.NewGuid()).Options))
    {
        public bool FailOnReport { get; init; }

        public override Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosAcademicosProgramacionResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", Inscripcion = "FALSE" },
            ]);

        public override Task<List<SpRecAreasCatedrasResult>?> SpRec_AreasCatedrasAsync(
            int? conTodo, int? enIngles,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAreasCatedrasResult>?>([
                new() { Orden = 1, Codigo = "%", Descripcion = "Todo" },
            ]);

        public override Task<List<SpRecProgramacionDocenteResumidaResult>?> SpRec_ProgramacionDocenteResumidaAsync(
            string? periodo, string? coordinacion, string? area,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (FailOnReport) throw new InvalidOperationException("El informe no debe consultarse.");
            return Task.FromResult<List<SpRecProgramacionDocenteResumidaResult>?>([
                new()
                {
                    Curso = "ENECO660/01", Nom_Catedra = "ELECTIVO", Profesor = "ANA PÉREZ",
                    Clases_Catedra = "Lun 10:00", Clases_Ayudantia = "Mar 12:00",
                    Salas_Catedra = "Sala 1", Salas_Ayudantia = "Sala 2", Vacantes = 20,
                    Inscritos = 15, Area = "Economía", RutProfesor = "12.345.678-9",
                },
            ]);
        }
    }
}
