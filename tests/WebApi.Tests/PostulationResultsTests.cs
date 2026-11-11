using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationFilters;
using Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationProcesses;
using Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationResultsByCourse;
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

public sealed class PostulationResultsTests
{
    [Fact]
    public async Task Query_maps_all_legacy_columns_for_the_given_origin()
    {
        var response = await new GetPostulationResultsByCourseHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetPostulationResultsByCourse("2024A", "INSCRIPCION",
                "2024-01-01T08:00", "2024-01-05T18:00"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("ENECO660/01", "Electivo de mención", "IC", 30, 2, 28, 10, 8, 8, 10, 20, 1, 1, 1, 0, 0, 1),
            (row.Course, row.CourseName, row.StudentType, row.OriginalVacancies, row.OtherAccepted,
                row.ProcessVacancies, row.Postulations, row.Accepted, row.AcceptedProcess,
                row.TotalOccupied, row.AvailableVacancies, row.VacancyRejections,
                row.RealVacancyRejections, row.ScheduleCapRejections, row.LoadRejections,
                row.RequirementRejections, row.OtherRejections));
        Assert.Equal(("2024A", "INSCRIPCION"), (response.Data.Period, response.Data.Origin));
    }

    [Fact]
    public async Task Endpoints_require_report_access_and_return_the_three_catalogs()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync(FiltersUrl)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        var filters = await client.GetFromJsonAsync<ApiResponse<PostulationFiltersDTO>>(FiltersUrl);
        Assert.Equal("2024A", Assert.Single(filters!.Data.Periods).Value);

        var processes = await client.GetFromJsonAsync<ApiResponse<PostulationProcessesDTO>>(ProcessesUrl);
        var process = Assert.Single(processes!.Data.Processes);
        Assert.Equal(("1", "Postulación", "2024-01-01T08:00", "2024-01-05T18:00"),
            (process.Value, process.Text, process.Start, process.End));

        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<PostulationResultsReportDTO>>();
        Assert.Equal("ENECO660/01", Assert.Single(body!.Data.Rows).Course);
    }

    [Fact]
    public async Task Invalid_origin_is_rejected_without_querying_the_report()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(
            "/api/reports/academic-enrollment/postulation-results-by-course/report?origin=OTHER&period=2024A&from=2024-01-01&to=2024-01-02");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/postulation-results-by-course/filters";
    private const string ProcessesUrl =
        "/api/reports/academic-enrollment/postulation-results-by-course/processes?origin=INSCRIPCION&period=2024A";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/postulation-results-by-course/report?origin=INSCRIPCION&period=2024A&from=2024-01-01T08:00&to=2024-01-05T18:00";

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
            .UseInMemoryDatabase("postulation-results-" + Guid.NewGuid()).Options))
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
                new() { IdProcesoPeriodo = 1, Nombre = "Postulación",
                    FechaInicio = new DateTime(2024, 1, 1, 8, 0, 0), FechaTermino = new DateTime(2024, 1, 5, 18, 0, 0) },
            ]);

        public override Task<List<SpRecResPostulaAgregaXCursoResult>?> SpRec_ResPostulaAgregaXCursoAsync(
            string? periodo, string? origen, DateTime? desde, DateTime? hasta,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecResPostulaAgregaXCursoResult>?>([
                new() { Curso = "ENECO660/01", Nom_Catedra = "Electivo de mención", Tipo_Alumno = "IC",
                    VacOrig = 30, AcepOtros = 2, VacProc = 28, MovProc = 10, AcepProc = 8, AcepTot = 10,
                    VacDisp = 20, RecVac = 1, RecVacReal = 1, RecTope = 1, RecCarga = 0, RecReq = 0, RecOtros = 1,
                    Periodo = "2024A", Cod_Catedra = "ENECO660", Cod_Seccion = "01", Origen = "INSCRIPCION" },
            ]);
    }
}
