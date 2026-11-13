using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;
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

public sealed class TeacherAvailabilityTests
{
    [Fact]
    public async Task Query_maps_rows_and_counts()
    {
        var response = await new GetTeacherAvailabilityHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetTeacherAvailability(null, "profesor"), default);

        Assert.Equal(("2024A", "2024 Primer Semestre", 1, 0),
            (response.Data.Period, response.Data.PeriodName, response.Data.Pending, response.Data.Ok));
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("ENECO660/01", "ELECTIVO", "P1", "ANA PÉREZ", "2", "1",
            "Lun 10:00", "2024-03-01", "PENDIENTE", "ana@fen.uchile.cl", "0", "Sin observaciones"),
            (row.Code, row.Course, row.TeacherCode, row.Teacher, row.Necessary, row.Given,
                row.Availability, row.CreatedAt, row.State, row.Mail, row.WelcomeEmail, row.Observations));
    }

    [Fact]
    public async Task Observations_query_returns_periods_and_rows()
    {
        var response = await new GetTeacherAvailabilityObservationsHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetTeacherAvailabilityObservations(null), default);

        Assert.Equal("2024A", response.Data.Period);
        Assert.Equal("2024 Primer Semestre", Assert.Single(response.Data.Periods).Text);
        Assert.Equal("Requiere sala especial", Assert.Single(response.Data.Rows).Observations);
    }

    [Fact]
    public async Task Endpoints_require_the_availability_permission()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ReportUrl)).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(ReportUrl)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_AVAILABILITY"));
        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<TeacherAvailabilityReportDTO>>();
        Assert.Equal("P1", Assert.Single(body!.Data.Rows).TeacherCode);

        using var observations = await client.GetAsync(ObservationsUrl);
        Assert.Equal(HttpStatusCode.OK, observations.StatusCode);
        var observationBody = await observations.Content
            .ReadFromJsonAsync<ApiResponse<TeacherAvailabilityObservationsReportDTO>>();
        Assert.Single(observationBody!.Data.Rows);
    }

    private const string ReportUrl =
        "/api/reports/academic-enrollment/teacher-availability/report";
    private const string ObservationsUrl =
        "/api/reports/academic-enrollment/teacher-availability/observations";

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
            .UseInMemoryDatabase("teacher-availability-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecPeriodoDisponibilidadResult>?> SpRec_PeriodoDisponibilidadAsync(
            string? uname, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodoDisponibilidadResult>?>([
                new() { Periodo = "2024A", NombrePeriodo = "2024 Primer Semestre", Habilitado = 1, EsProfesor = 1 },
            ]);

        public override Task<List<SpRecFEProgramacionDocenteResult>?> SpRec_FE_ProgramacionDocenteAsync(
            string? periodo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecFEProgramacionDocenteResult>?>([
                new() { Codigo = "ENECO660/01", Catedra = "ELECTIVO", Cod_Profesor = "P1",
                    Profesor = "ANA PÉREZ", Necesarias = 2, Dadas = 1, Disp = "Lun 10:00",
                    FechaCreacion = "2024-03-01", Estado = "PENDIENTE", Mail = "ana@fen.uchile.cl",
                    CorreoBienvenida = 0, Observaciones = "Sin observaciones" },
            ]);

        public override Task<List<SpRecPeriodosAcademicosResult>?> SpRec_PeriodosAcademicosAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosAcademicosResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", Inscripcion = "FALSE" },
            ]);

        public override Task<List<SpRecFEDisponibilidadObservacionesResult>?> SpRec_FE_DisponibilidadObservacionesAsync(
            string? periodo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecFEDisponibilidadObservacionesResult>?>([
                new() { Cod_Profesor = "P1", Profesor = "ANA PÉREZ", Observaciones = "Requiere sala especial" },
            ]);
    }
}
