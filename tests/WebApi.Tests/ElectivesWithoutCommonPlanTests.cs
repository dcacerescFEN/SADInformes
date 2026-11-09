using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.ElectivesWithoutCommonPlan.Queries.GetElectivesWithoutCommonPlan;
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

public sealed class ElectivesWithoutCommonPlanTests
{
    [Fact]
    public async Task Query_maps_the_three_legacy_columns()
    {
        var response = await new GetElectivesWithoutCommonPlanHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetElectivesWithoutCommonPlan(), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("009615/2003", "ENECO660", "Electivo de mención"),
            (row.Decree, row.CourseCode, row.CourseName));
        Assert.Equal(1, response.Data.Total);
    }

    [Fact]
    public async Task Report_requires_the_report_module_permission()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync(Url)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE"));
        Assert.Equal((int)HttpStatusCode.Forbidden,
            (int)(await client.GetAsync(Url)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        using var response = await client.GetAsync(Url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ElectivesWithoutCommonPlanReportDTO>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal("ENECO660", Assert.Single(body.Data.Rows).CourseCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    private const string Url =
        "/api/reports/academic-enrollment/electives-without-common-plan/report";

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
            .UseInMemoryDatabase("electives-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecElectivosDeMencionSinPlanComunResult>?> SpRec_ElectivosDeMencionSinPlanComunAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecElectivosDeMencionSinPlanComunResult>?>([
                new() { Decreto2 = "009615/2003", Cod_Catedra = "ENECO660", Nom_Catedra = "Electivo de mención" },
            ]);
    }
}
