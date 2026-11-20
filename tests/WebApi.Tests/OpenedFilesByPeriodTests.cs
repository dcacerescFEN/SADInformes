using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Regulation.ExpedientesXPeriodo.Queries.GetOpenedFilesByPeriod;
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

public sealed class OpenedFilesByPeriodTests
{
    private const string Route = "/api/reports/regulation/opened-files-by-period/report";
    private const string Range = "?from=2024-03-01&to=2024-03-31";
    private const string Access = "PERMISSION_REPORT_ACCESS_REPORT_MODULE";
    private const string View = "PERMISSION_REPORT_VIEW_REGULATION";

    [Fact]
    public async Task Report_passes_the_normalized_range_and_maps_every_legacy_column()
    {
        var procedures = new Procedures();
        var response = await new GetOpenedFilesByPeriodHandler(procedures, new ApiResponseService())
            .Handle(new GetOpenedFilesByPeriod("2024-03-01", "2024-03-31"), default);

        Assert.True(procedures.Called);
        Assert.Equal(("20240301", "20240331"),
            (procedures.ReceivedFrom, procedures.ReceivedTo));
        Assert.Equal(("2024-03-01", "2024-03-31"), (response.Data.From, response.Data.To));
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(
            ("A1", "T1", "P1", "ANA PÉREZ", "TÍTULO", "PREGRADO JC", "EGRESADO MOTIVO",
                "20161", "01-03-2024", "912345678", "aperez@fen.uchile.cl", "ana@example.com"),
            (row.StudentCode, row.TitleCode, row.PersonCode, row.Name, row.DegreeTitle,
                row.Career, row.Situation, row.Promotion, row.OpeningDate, row.Phone,
                row.FenEmail, row.PersonalEmail));
    }

    [Fact]
    public async Task Report_keeps_missing_values_empty()
    {
        var procedures = new Procedures { Empty = true };
        var response = await new GetOpenedFilesByPeriodHandler(procedures, new ApiResponseService())
            .Handle(new GetOpenedFilesByPeriod("2024-03-01", "2024-03-31"), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("", "", "", "", "", "", "", "", "", "", "", ""),
            (row.StudentCode, row.TitleCode, row.PersonCode, row.Name, row.DegreeTitle,
                row.Career, row.Situation, row.Promotion, row.OpeningDate, row.Phone,
                row.FenEmail, row.PersonalEmail));
    }

    [Theory]
    [InlineData(null, "2024-03-31")]
    [InlineData("2024-03-01", null)]
    [InlineData("", "")]
    [InlineData("2024-13-01", "2024-03-31")]
    [InlineData("2024-03-01", "31-03-2024")]
    [InlineData("01/03/2024", "2024-03-31")]
    public async Task Report_rejects_a_missing_or_invalid_range_without_calling_the_procedure(
        string? from, string? to)
    {
        var procedures = new Procedures();
        var handler = new GetOpenedFilesByPeriodHandler(procedures, new ApiResponseService());

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new GetOpenedFilesByPeriod(from, to), default));

        Assert.Equal("El rango de fechas no es válido.", error.Message);
        Assert.False(procedures.Called);
    }

    [Fact]
    public async Task Report_rejects_a_reversed_range_without_calling_the_procedure()
    {
        var procedures = new Procedures();
        var handler = new GetOpenedFilesByPeriodHandler(procedures, new ApiResponseService());

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new GetOpenedFilesByPeriod("2024-03-31", "2024-03-01"), default));

        Assert.Equal("El rango de fechas no es válido.", error.Message);
        Assert.False(procedures.Called);
    }

    [Fact]
    public async Task Endpoint_rejects_requests_without_a_token()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();

        using var response = await client.GetAsync(Route + Range);

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

        using var response = await client.GetAsync(Route + Range);

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

        using var response = await client.GetAsync(Route + Range);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(("2024-03-01", "2024-03-31"),
            (data.GetProperty("from").GetString(), data.GetProperty("to").GetString()));
        var row = Assert.Single(data.GetProperty("rows").EnumerateArray());
        Assert.Equal(
            new[] { "A1", "T1", "P1", "ANA PÉREZ", "TÍTULO", "PREGRADO JC", "EGRESADO MOTIVO",
                "20161", "01-03-2024", "912345678", "aperez@fen.uchile.cl", "ana@example.com" },
            new[] { "studentCode", "titleCode", "personCode", "name", "degreeTitle",
                "career", "situation", "promotion", "openingDate", "phone", "fenEmail",
                "personalEmail" }
                .Select(name => row.GetProperty(name).GetString()));
    }

    [Fact]
    public async Task Endpoint_rejects_an_invalid_range_with_a_bad_request()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));

        using var response = await client.GetAsync(Route + "?from=2024-03-31&to=2024-03-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El rango de fechas no es válido.", body);
        Assert.False(procedures.Called);
    }

    [Fact]
    public async Task Endpoint_sanitizes_procedure_failures()
    {
        var procedures = new Procedures { Fail = true };
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));

        using var response = await client.GetAsync(Route + Range);

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
            .UseInMemoryDatabase("opened-files-period-" + Guid.NewGuid()).Options)) { }

        public bool Called { get; private set; }
        public string? ReceivedFrom { get; private set; }
        public string? ReceivedTo { get; private set; }
        public bool Fail { get; init; }
        public bool Empty { get; init; }

        public override Task<List<SpRecExpedientesAbiertosXPeriodoResult>?> SpRec_ExpedientesAbiertosXPeriodoAsync(
            string? desde, string? hasta,
            OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Called = true;
            ReceivedFrom = desde;
            ReceivedTo = hasta;
            if (Fail) throw new InvalidOperationException("private-sql");
            if (Empty)
            {
                return Task.FromResult<List<SpRecExpedientesAbiertosXPeriodoResult>?>([
                    new() { Codigo = " ", Nombre = null },
                ]);
            }
            return Task.FromResult<List<SpRecExpedientesAbiertosXPeriodoResult>?>([
                new()
                {
                    Codigo = " A1 ", Cod_Titulo = " T1 ", Rut = " P1 ",
                    Nombre = " ANA PÉREZ ", TituloGrado = " TÍTULO ",
                    Carrera = " PREGRADO JC ", Situacion = " EGRESADO MOTIVO ",
                    Promocion = " 20161 ", FechaApertura = " 01-03-2024 ",
                    Telefono = " 912345678 ", CorreoFen = " aperez@fen.uchile.cl ",
                    CorreoPersonal = " ana@example.com ",
                },
            ]);
        }
    }
}
