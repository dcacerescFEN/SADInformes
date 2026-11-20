using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Regulation.ExpedientesNoAbiertos.Queries.GetUnopenedFiles;
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

public sealed class UnopenedFilesTests
{
    private const string Route = "/api/reports/regulation/unopened-files/report";
    private const string Access = "PERMISSION_REPORT_ACCESS_REPORT_MODULE";
    private const string View = "PERMISSION_REPORT_VIEW_REGULATION";

    [Fact]
    public async Task Report_maps_and_trims_every_legacy_column()
    {
        var procedures = new Procedures();
        var response = await new GetUnopenedFilesHandler(procedures, new ApiResponseService())
            .Handle(new GetUnopenedFiles(), default);

        Assert.True(procedures.Called);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(
            ("A1", "T1", "P1", "ANA PÉREZ", "TÍTULO", "PREGRADO JC", "EGRESADO MOTIVO",
                "20161", "912345678", "aperez@fen.uchile.cl", "ana@example.com"),
            (row.StudentCode, row.TitleCode, row.PersonCode, row.Name, row.DegreeTitle,
                row.Career, row.Situation, row.Promotion, row.Phone, row.FenEmail,
                row.PersonalEmail));
    }

    [Fact]
    public async Task Report_keeps_missing_values_empty()
    {
        var procedures = new Procedures { Empty = true };
        var response = await new GetUnopenedFilesHandler(procedures, new ApiResponseService())
            .Handle(new GetUnopenedFiles(), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("", "", "", "", "", "", "", "", "", "", ""),
            (row.StudentCode, row.TitleCode, row.PersonCode, row.Name, row.DegreeTitle,
                row.Career, row.Situation, row.Promotion, row.Phone, row.FenEmail,
                row.PersonalEmail));
    }

    [Fact]
    public async Task Endpoint_rejects_requests_without_a_token()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();

        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(procedures.Called);
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
        Assert.False(procedures.Called);
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
        var row = Assert.Single(json.RootElement.GetProperty("data").GetProperty("rows").EnumerateArray());
        Assert.Equal(
            new[] { "A1", "T1", "P1", "ANA PÉREZ", "TÍTULO", "PREGRADO JC", "EGRESADO MOTIVO",
                "20161", "912345678", "aperez@fen.uchile.cl", "ana@example.com" },
            new[] { "studentCode", "titleCode", "personCode", "name", "degreeTitle",
                "career", "situation", "promotion", "phone", "fenEmail", "personalEmail" }
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
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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
        return (factory, tokens);
    }

    internal sealed class Procedures : SadContextProcedures
    {
        public Procedures() : base(new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("unopened-files-" + Guid.NewGuid()).Options)) { }

        public bool Called { get; private set; }
        public bool Fail { get; init; }
        public bool Empty { get; init; }

        public override Task<List<SpRecExpedientesNoAbiertosResult>?> SpRec_ExpedientesNoAbiertosAsync(
            OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Called = true;
            if (Fail) throw new InvalidOperationException("private-sql");
            if (Empty)
            {
                return Task.FromResult<List<SpRecExpedientesNoAbiertosResult>?>([
                    new() { Codigo = " ", Nombre = null },
                ]);
            }
            return Task.FromResult<List<SpRecExpedientesNoAbiertosResult>?>([
                new()
                {
                    Codigo = " A1 ", Cod_Titulo = " T1 ", Rut = " P1 ", Nombre = " ANA PÉREZ ",
                    TituloGrado = " TÍTULO ", Carrera = " PREGRADO JC ",
                    Situacion = " EGRESADO MOTIVO ", Promocion = " 20161 ",
                    Telefono = " 912345678 ", CorreoFen = " aperez@fen.uchile.cl ",
                    CorreoPersonal = " ana@example.com ",
                },
            ]);
        }
    }
}
