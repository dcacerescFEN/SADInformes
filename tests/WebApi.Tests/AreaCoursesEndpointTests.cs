using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class AreaCoursesEndpointTests
{
    private const string Route = "/api/reports/postgraduate/area-courses";
    private const string Query = "?year1=2024&year2=2026";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_AREA_COURSES";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(AreaCoursesTests.Procedures procedures)
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

    [Fact]
    public async Task Filters_and_report_are_not_cacheable()
    {
        var (factory, tokens) = Create(new AreaCoursesTests.Procedures());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var filters = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.True(filters.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await filters.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "2026", "2024" }, json.RootElement.GetProperty("data").GetProperty("years")
            .EnumerateArray().Select(row => row.GetString()));
        using var report = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True(report.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES", 403)]
    public async Task All_routes_require_the_report_permission(string? role, int status)
    {
        var procedures = new AreaCoursesTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters", Query, "/download" + Query })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Download_has_twenty_eight_columns_and_legacy_values()
    {
        var (factory, tokens) = Create(new AreaCoursesTests.Procedures());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AlumnosPregradoPostgrado.xlsx", response.Content.Headers.ContentDisposition!.ToString());
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(xml); var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(new[] { "ID", "Area_Catedra", "Area", "Periodo", "Cod_Catedra", "Seccion",
            "Creditos", "Nombre_Profesor", "Cod_Persona", "Tipo_Profesor", "Inicio_Catedra",
            "Termino_Catedra", "Tipo_Alumno", "Modalidad", "Promocion", "Nombre_Catedra",
            "TipoCatedra", "Tipo_Horario", "AQPQ", "Categorización_Docente", "Confirmado",
            "HorasDedicadas", "Alumnos_Totales", "Alumnos_Postgrado", "Alumnos_Pregrado",
            "Promedio_Ev_Docente", "Observaciones Curso", "Usuario_Observ" },
            rows[0].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Equal(new[] { "2", "Gestión", "ADM", "2024F", "MBA600", "01", "4", "Ana Pérez",
            "P/1", "FULL TIME", "01/03/2024", "30/06/2024", "MBA", "SUNRISE", "2024",
            "Gestión Estratégica", "Cátedra", "Diurno", "AQ", "SA", "Confirmado", "4.5",
            "20", "15", "5", "6,25", "Observación", "usuario 01-03-2024 10:00" },
            rows[1].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Empty(sheet.Descendants(ns + "f"));
        Assert.All(sheet.Descendants(ns + "c"), cell => Assert.Equal("inlineStr", cell.Attribute("t")!.Value));
        Assert.All(rows, row => Assert.Equal(28, row.Elements(ns + "c").Count()));
    }

    [Fact]
    public async Task Invalid_year_never_calls_the_procedure()
    {
        var procedures = new AreaCoursesTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal((int)HttpStatusCode.BadRequest,
            (int)(await client.GetAsync(Route + "?year1=Todos&year2=2024")).StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_export_has_no_attachment()
    {
        var (factory, tokens) = Create(new AreaCoursesTests.Procedures { Empty = true });
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Sin Datos", await response.Content.ReadAsStringAsync());
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Procedure_errors_are_sanitized()
    {
        var (factory, tokens) = Create(new AreaCoursesTests.Procedures { Fail = true });
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }
}
