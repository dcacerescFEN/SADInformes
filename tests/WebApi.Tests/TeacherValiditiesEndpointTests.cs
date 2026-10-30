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

public sealed class TeacherValiditiesEndpointTests
{
    private const string Route = "/api/reports/postgraduate/teacher-validities";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_VALIDITIES";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(TeacherValiditiesTests.Procedures procedures)
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
        var (factory, tokens) = Create(new TeacherValiditiesTests.Procedures());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var filters = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.True(filters.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await filters.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "2026", "2024" }, json.RootElement.GetProperty("data").GetProperty("years")
            .EnumerateArray().Select(row => row.GetString()));
        using var report = await client.GetAsync(Route + "?year=2024");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True(report.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES", 403)]
    public async Task All_routes_require_the_report_permission(string? role, int status)
    {
        var procedures = new TeacherValiditiesTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters", "?year=2024", "/download?year=2024" })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Download_has_fourteen_columns_validity_labels_and_no_formulas()
    {
        var (factory, tokens) = Create(new TeacherValiditiesTests.Procedures());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("ProfesoresVigencias.xlsx", response.Content.Headers.ContentDisposition!.ToString());
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(xml); var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(new[] { "ID", "Año", "Periodo", "Cod_Catedra", "Seccion", "Catedra",
            "Tipo_Alumno", "Modalidad", "Promocion", "Cod_Persona", "Nombre", "Email1",
            "Horas_Dedicadas", "Vigencia" }, rows[0].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Equal(new[] { "2", "2024", "2024F", "MBA600", "01", "Gestión", "MBA",
            "SUNRISE", "2024", "P/1", "Ana Pérez", "ana@fen.uchile.cl", "4.5", "Vigente" },
            rows[1].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Empty(sheet.Descendants(ns + "f"));
        Assert.All(sheet.Descendants(ns + "c"), cell => Assert.Equal("inlineStr", cell.Attribute("t")!.Value));
        Assert.All(rows, row => Assert.Equal(14, row.Elements(ns + "c").Count()));
    }

    [Fact]
    public async Task Invalid_year_never_calls_the_procedure()
    {
        var procedures = new TeacherValiditiesTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal((int)HttpStatusCode.BadRequest, (int)(await client.GetAsync(Route + "?year=Todos")).StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_export_has_no_attachment()
    {
        var (factory, tokens) = Create(new TeacherValiditiesTests.Procedures { Empty = true });
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download?year=2024");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Sin Datos", await response.Content.ReadAsStringAsync());
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Procedure_errors_are_sanitized()
    {
        var (factory, tokens) = Create(new TeacherValiditiesTests.Procedures { Fail = true });
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "?year=2024");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }
}
