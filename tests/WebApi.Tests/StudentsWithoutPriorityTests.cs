using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.StudentsWithoutPriority.Queries.GetStudentsWithoutPriority;
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

public sealed class StudentsWithoutPriorityTests
{
    [Fact]
    public async Task Query_maps_the_four_legacy_columns()
    {
        var response = await new GetStudentsWithoutPriorityHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetStudentsWithoutPriority(), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "Ana Pérez", "Regular", "IC"),
            (row.StudentCode, row.Name, row.AcademicStatus, row.StudentType));
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
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StudentsWithoutPriorityReportDTO>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal("A/1", Assert.Single(body.Data.Rows).StudentCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    private const string Url =
        "/api/reports/academic-enrollment/students-without-priority/report";

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
            .UseInMemoryDatabase("students-without-priority-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecAlumnosSinPrioridadResult>?> SpRec_AlumnosSinPrioridadAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAlumnosSinPrioridadResult>?>([
                new() { Cod_Alumno = "A/1", Nombre = "Ana Pérez", Cod_SitAcademica = "Regular", Tipo_Alumno = "IC" },
            ]);
    }
}
