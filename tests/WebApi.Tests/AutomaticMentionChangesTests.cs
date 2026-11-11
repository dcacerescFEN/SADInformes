using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.AutomaticMentionChanges.Queries.GetAutomaticMentionChanges;
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
using Xunit;

namespace WebApi.Tests;

public sealed class AutomaticMentionChangesTests
{
    [Fact]
    public async Task Query_maps_the_three_legacy_columns()
    {
        var response = await new GetAutomaticMentionChangesHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetAutomaticMentionChanges("2024A"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "Ana Pérez", "MA"),
            (row.StudentCode, row.Name, row.Mention));
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
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<AutomaticMentionChangesReportDTO>>();
        Assert.Equal("MA", Assert.Single(body!.Data.Rows).Mention);
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
            "/api/reports/academic-enrollment/automatic-mention-changes/report?period=");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private const string FiltersUrl =
        "/api/reports/academic-enrollment/automatic-mention-changes/filters";
    private const string ReportUrl =
        "/api/reports/academic-enrollment/automatic-mention-changes/report?period=2024A";

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
            .UseInMemoryDatabase("automatic-mention-changes-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecPeriodosActivosTomaRamosResult>?> SpRec_PeriodosActivosTomaRamosAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosActivosTomaRamosResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", PeriodoExtraordinario = false },
            ]);

        public override Task<List<SpRecAlumnosCambioMencionAutomaticoResult>?> SpRec_AlumnosCambioMencionAutomaticoAsync(
            string? periodo, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAlumnosCambioMencionAutomaticoResult>?>([
                new() { Cod_Alumno = "A/1", Nombre = "Ana Pérez", Mencion = "MA" },
            ]);
    }
}
