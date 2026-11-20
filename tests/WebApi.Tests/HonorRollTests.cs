using System.Net;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRoll;
using Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRollFilters;
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

public sealed class HonorRollTests
{
    private const string FiltersRoute = "/api/reports/others/honor-roll/filters";
    private const string ReportRoute =
        "/api/reports/others/honor-roll/report?career=IC&entryYear=2019&honorYear=2020";
    private const string Access = "PERMISSION_REPORT_ACCESS_REPORT_MODULE";
    private const string Section = "PERMISSION_REPORT_VIEW_OTHER";
    private const string Honor = "PERMISSION_REPORT_VIEW_HONOR_ROLL";

    [Fact]
    public async Task Filters_lists_the_legacy_careers_and_years_without_touching_the_database()
    {
        var procedures = new Procedures();
        var response = await new GetHonorRollFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetHonorRollFilters(), default);

        Assert.Equal(("NO", "Todos"), (procedures.ReceivedIncludeAll, procedures.ReceivedStudentType));
        Assert.Equal(
            new[] { ("IC", "Ingeniería Comercial"), ("IICG", "Ingeniería en Información y Control de Gestión y Auditoría"), ("CA", "Contador Auditor") },
            response.Data.Careers.Select(career => (career.Value, career.Text)).ToArray());
        Assert.Equal(new[] { ("2019", "2019"), ("2020", "2020") },
            response.Data.Years.Select(year => (year.Value, year.Text)).ToArray());
    }

    [Fact]
    public async Task Report_maps_every_legacy_column_and_passes_the_three_procedure_parameters()
    {
        var procedures = new Procedures();
        var response = await new GetHonorRollHandler(procedures, new ApiResponseService())
            .Handle(new GetHonorRoll("ic", " 2019 ", "2020"), default);

        Assert.Equal(("IC", "2019", "2020"),
            (procedures.ReceivedCareer, procedures.ReceivedEntryYear, procedures.ReceivedHonorYear));
        Assert.Equal(("IC", "2019", "2020"),
            (response.Data.Career, response.Data.EntryYear, response.Data.HonorYear));

        Assert.Collection(
            response.Data.Rows,
            row => Assert.Equal(
                (1, "ANA PÉREZ", "12345678", "A1", "20191", "REGULAR", "5.5", "1"),
                (row.Position, row.Name, row.Rut, row.StudentCode, row.EntrySemester,
                    row.Situation, row.Average, row.Percentile)),
            row => Assert.Equal(
                (2, "BETO SOTO", "87654321", "A2", "", "", "6", "0.8333"),
                (row.Position, row.Name, row.Rut, row.StudentCode, row.EntrySemester,
                    row.Situation, row.Average, row.Percentile)),
            row => Assert.Equal(
                (3, "CARLA DÍAZ", "", "A3", "", "", "", ""),
                (row.Position, row.Name, row.Rut, row.StudentCode, row.EntrySemester,
                    row.Situation, row.Average, row.Percentile)));
    }

    [Theory]
    [InlineData("OTRO", "2019", "2020", "La carrera no es válida.")]
    [InlineData(null, "2019", "2020", "La carrera no es válida.")]
    [InlineData("IC", "201", "2020", "El año de ingreso no es válido.")]
    [InlineData("IC", "20190", "2020", "El año de ingreso no es válido.")]
    [InlineData("IC", "20a9", "2020", "El año de ingreso no es válido.")]
    [InlineData("IC", null, "2020", "El año de ingreso no es válido.")]
    [InlineData("IC", "2019", "202a", "El año del cuadro de honor no es válido.")]
    [InlineData("IC", "2019", "20201", "El año del cuadro de honor no es válido.")]
    [InlineData("IC", "2019", "-020", "El año del cuadro de honor no es válido.")]
    public async Task Invalid_filters_are_rejected_without_calling_the_procedure(
        string? career, string? entryYear, string? honorYear, string message)
    {
        var procedures = new Procedures();
        var handler = new GetHonorRollHandler(procedures, new ApiResponseService());

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new GetHonorRoll(career, entryYear, honorYear), default));

        Assert.Equal(message, error.Message);
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_rejects_requests_without_a_token()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();

        using var filters = await client.GetAsync(FiltersRoute);
        using var report = await client.GetAsync(ReportRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, filters.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, report.StatusCode);
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_rejects_the_regulation_permissions_without_the_honor_roll_permission()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, Section));

        using var filters = await client.GetAsync(FiltersRoute);
        using var report = await client.GetAsync(ReportRoute);

        Assert.Equal(HttpStatusCode.Forbidden, filters.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, report.StatusCode);
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_returns_filters_and_the_report_envelope_and_is_not_cacheable()
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, Section, Honor));

        using var filters = await client.GetAsync(FiltersRoute);
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.True(filters.Headers.CacheControl!.NoStore);
        using var filtersJson = JsonDocument.Parse(await filters.Content.ReadAsStringAsync());
        Assert.True(filtersJson.RootElement.GetProperty("success").GetBoolean());
        var data = filtersJson.RootElement.GetProperty("data");
        Assert.Equal(
            new[] { "IC", "IICG", "CA" },
            data.GetProperty("careers").EnumerateArray()
                .Select(career => career.GetProperty("value").GetString()).ToArray());
        Assert.Equal(
            new[] { "2019", "2020" },
            data.GetProperty("years").EnumerateArray()
                .Select(year => year.GetProperty("value").GetString()).ToArray());

        using var report = await client.GetAsync(ReportRoute);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True(report.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var reportData = json.RootElement.GetProperty("data");
        Assert.Equal(new[] { "IC", "2019", "2020" },
            new[] { "career", "entryYear", "honorYear" }
                .Select(name => reportData.GetProperty(name).GetString()));
        var row = reportData.GetProperty("rows").EnumerateArray().First();
        Assert.Equal(new[] { "ANA PÉREZ", "12345678", "A1", "20191", "REGULAR", "5.5", "1" },
            new[] { "name", "rut", "studentCode", "entrySemester", "situation", "average", "percentile" }
                .Select(name => row.GetProperty(name).GetString()));
        Assert.Equal(1, row.GetProperty("position").GetInt32());
        Assert.Equal(("IC", "2019", "2020"),
            (procedures.ReceivedCareer, procedures.ReceivedEntryYear, procedures.ReceivedHonorYear));
    }

    [Theory]
    [InlineData("career=OTRO&entryYear=2019&honorYear=2020", "La carrera no es válida.")]
    [InlineData("career=IC&entryYear=&honorYear=2020", "El año de ingreso no es válido.")]
    [InlineData("career=IC&entryYear=2019&honorYear=20", "El año del cuadro de honor no es válido.")]
    public async Task Endpoint_rejects_invalid_filters_with_a_bad_request(string query, string message)
    {
        var procedures = new Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, Section, Honor));

        using var response = await client.GetAsync(
            "/api/reports/others/honor-roll/report?" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
        Assert.Equal(0, procedures.Calls);
    }

    [Fact]
    public async Task Endpoint_sanitizes_procedure_failures()
    {
        var procedures = new Procedures { Fail = true };
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, Section, Honor));

        using var report = await client.GetAsync(ReportRoute);
        using var filters = await client.GetAsync(FiltersRoute);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, report.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, filters.StatusCode);
        var body = await report.Content.ReadAsStringAsync();
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

    private sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("honor-roll-" + Guid.NewGuid()).Options))
    {
        public int Calls { get; private set; }
        public string? ReceivedIncludeAll { get; private set; }
        public string? ReceivedStudentType { get; private set; }
        public string? ReceivedCareer { get; private set; }
        public string? ReceivedEntryYear { get; private set; }
        public string? ReceivedHonorYear { get; private set; }
        public bool Fail { get; init; }

        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Calls++;
            ReceivedIncludeAll = incluirTodos;
            ReceivedStudentType = tipoAlumno;
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([
                new() { Año = " 2019 ", Orden = "1" },
                new() { Año = "2019", Orden = "1" },
                new() { Año = "2020", Orden = "2" },
                new() { Año = "202", Orden = "3" },
                new() { Año = "ABCD", Orden = "4" },
                new() { Año = null, Orden = "5" },
            ]);
        }

        public override Task<List<SpRecAlumnosCuadroDeHonorResult>?> SpRec_AlumnosCuadroDeHonorAsync(
            string? carrera, string? añoIngreso, string? año,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Calls++;
            ReceivedCareer = carrera;
            ReceivedEntryYear = añoIngreso;
            ReceivedHonorYear = año;
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecAlumnosCuadroDeHonorResult>?>([
                new()
                {
                    Posicion = 1, Nombre = " ANA PÉREZ ", Rut = " 12345678 ", Código = " A1 ",
                    Ingreso = " 20191 ", Tipo = " PACE ", Colegio = " LICEO A ",
                    Puntaje = 700.5, Situación = " REGULAR ", Promedio = 5.5m,
                    Percentil = 1.0000m,
                },
                new()
                {
                    Posicion = 2, Nombre = "BETO SOTO", Rut = "87654321", Código = "A2",
                    Ingreso = "", Tipo = "", Colegio = "", Puntaje = null, Situación = "",
                    Promedio = 6.000m, Percentil = 0.8333333m,
                },
                new()
                {
                    Posicion = 3, Nombre = "CARLA DÍAZ", Rut = null, Código = "A3",
                    Promedio = null, Percentil = null,
                },
            ]);
        }
    }
}
