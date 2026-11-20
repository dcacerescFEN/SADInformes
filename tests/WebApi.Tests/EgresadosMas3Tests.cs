using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Regulation.EgresadosMas3.Queries.GetGraduatesOverThreeYears;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class EgresadosMas3Tests
{
    private const string Route = "/api/reports/regulation/graduates-over-3-years/report";
    private const string Access = "PERMISSION_REPORT_ACCESS_REPORT_MODULE";
    private const string View = "PERMISSION_REPORT_VIEW_REGULATION";

    [Fact]
    public async Task Report_maps_the_active_period_and_every_legacy_column()
    {
        var procedures = new Procedures();
        var response = await new GetGraduatesOverThreeYearsHandler(
            Sad(), procedures, new ApiResponseService())
            .Handle(new GetGraduatesOverThreeYears(), default);

        Assert.Equal("20242", procedures.ReceivedPeriod);
        Assert.Equal("20242", response.Data.Period);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(
            ("A1", "ANA PÉREZ", "PREGRADO", "3", "Decreto 3", "MENCIÓN", "INGENIERÍA",
                "EGRESADO MAS DE 3 ANOS", "20161", "2013"),
            (row.StudentCode, row.Name, row.StudentType, row.Decree, row.DecreeDescription,
                row.Mention, row.GraduatedFrom, row.Situation, row.EntrySemester, row.Promotion));
    }

    [Fact]
    public async Task Missing_active_period_is_rejected_without_calling_the_procedure()
    {
        var procedures = new Procedures();
        var handler = new GetGraduatesOverThreeYearsHandler(
            Sad(active: false), procedures, new ApiResponseService());

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new GetGraduatesOverThreeYears(), default));

        Assert.Equal("No hay un período académico activo.", error.Message);
        Assert.Null(procedures.ReceivedPeriod);
    }

    [Fact]
    public async Task Endpoint_rejects_requests_without_a_token()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();

        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(procedures.ReceivedPeriod);
    }

    [Fact]
    public async Task Endpoint_rejects_the_module_permission_without_the_regulation_permission()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access));

        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(procedures.ReceivedPeriod);
    }

    [Fact]
    public async Task Endpoint_returns_the_report_envelope_and_is_not_cacheable()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));

        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("20242", data.GetProperty("period").GetString());
        var row = Assert.Single(data.GetProperty("rows").EnumerateArray());
        Assert.Equal(new[] { "A1", "ANA PÉREZ", "PREGRADO", "3", "Decreto 3", "MENCIÓN",
                "INGENIERÍA", "EGRESADO MAS DE 3 ANOS", "20161", "2013" },
            new[] { "studentCode", "name", "studentType", "decree", "decreeDescription",
                "mention", "graduatedFrom", "situation", "entrySemester", "promotion" }
                .Select(name => row.GetProperty(name).GetString()));
    }

    [Fact]
    public async Task Endpoint_sanitizes_procedure_failures()
    {
        var procedures = new Procedures { Fail = true };
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));

        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }

    private static (WebApplicationFactory<Program>, TokenFactory) Create(Procedures procedures)
    {
        var sad = Sad();
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                services.AddSingleton<ISadContext>(sad);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    private static SadContext Sad(bool active = true)
    {
        var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("egresados-mas-3-sad-" + Guid.NewGuid()).Options);
        context.PeriodosAcademicos.Add(new PeriodoAcademico
        {
            Periodo = "20242", Tipo = active ? "SEMESTRAL" : "ANUAL",
            InicioPeriodo = active ? DateTime.Now.AddDays(-1) : DateTime.Now.AddYears(-1),
            FinPeriodo = active ? DateTime.Now.AddDays(1) : DateTime.Now.AddYears(-1).AddDays(100),
        });
        context.SaveChanges();
        return context;
    }

    private sealed class Procedures : SadContextProcedures
    {
        public Procedures() : base(new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("egresados-mas-3-" + Guid.NewGuid()).Options)) { }

        public string? ReceivedPeriod { get; private set; }
        public bool Fail { get; init; }

        public override Task<List<SpRecAlumnosEgresadosMasTresAnosResult>?> SpRec_AlumnosEgresadosMasTresAnosAsync(
            string? periodoActual, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            ReceivedPeriod = periodoActual;
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecAlumnosEgresadosMasTresAnosResult>?>([
                new()
                {
                    Cod_Alumno = " A1 ", Nombre = " ANA PÉREZ ", Tipo_Alumno = "PREGRADO",
                    Decreto = " 3 ", Decreto2 = " Decreto 3 ", Mencion = " MENCIÓN ",
                    EgresadoDe = " INGENIERÍA ", Situacion = " EGRESADO MAS DE 3 ANOS ",
                    Sem_Ingreso = " 20161 ", Promocion = " 2013 ",
                },
            ]);
        }
    }
}
