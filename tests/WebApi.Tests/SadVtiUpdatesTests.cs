using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class SadVtiUpdatesTests
{
    private const string Route = "/api/reports/postgraduate/sad-vti-updates";
    private const string Query = "?state=-1&from=2026-10-01&to=2026-10-01";
    private const string Permission = "PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES";
    private static async Task<(WebApplicationFactory<Program>, TokenFactory)> Create(bool unavailable = false)
    {
        var options = unavailable ? new DbContextOptions<SadContext>() : SadVtiUpdatesQueryTests.Options();
        if (!unavailable) { await using var seed = new SadContext(options); await SadVtiUpdatesQueryTests.Seed(seed); }
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => new SadContext(options));
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_STUDENT_REQUESTS", 403)]
    public async Task Both_routes_require_report_permission_before_reading_data(string? role, int status)
    {
        var (factory, tokens) = await Create(unavailable: true);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters", "/download" + Query })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
    }

    [Theory]
    [InlineData("", "La fecha desde no es válida.")]
    [InlineData("?from=2026-02-30&to=2026-10-01", "La fecha desde no es válida.")]
    [InlineData("?from=01-10-2026&to=2026-10-01", "La fecha desde no es válida.")]
    [InlineData("?from=2026-10-01", "La fecha hasta no es válida.")]
    [InlineData("?from=2026-10-02&to=2026-10-01", "La fecha desde no puede ser posterior a la fecha hasta.")]
    [InlineData("?from=2026-10-01&to=2026-10-01&state=", "El estado no es válido.")]
    public async Task Invalid_input_is_rejected_without_queries_or_attachment(string query, string message)
    {
        var (factory, tokens) = await Create(unavailable: true);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Permission));
        using var response = await client.GetAsync(Route + "/download" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Filters_return_legacy_Todos_and_state_descriptions()
    {
        var (factory, tokens) = await Create();
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Permission));
        using var response = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var states = json.RootElement.GetProperty("data").GetProperty("states");
        Assert.Equal(3, states.GetArrayLength());
        Assert.Equal("-1", states[0].GetProperty("value").GetString());
        Assert.Equal("Todos", states[0].GetProperty("text").GetString());
        Assert.Equal("Actualizado", states[1].GetProperty("text").GetString());
    }

    [Fact]
    public async Task Download_is_real_xlsx_with_exact_12_columns_and_inert_formula_text()
    {
        var (factory, tokens) = await Create();
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Permission));
        using var response = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("AudiPostgradoAlumnosGuia.xlsx", response.Content.Headers.ContentDisposition!.ToString());
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        foreach (var part in new[] { "[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "xl/worksheets/sheet1.xml" })
            Assert.NotNull(zip.GetEntry(part));
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var sheet = XDocument.Load(xml); XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(3, rows.Length);
        Assert.Equal(new[] { "Estado", "Descripcion", "Cod_Persona", "Cod_Alumno_Anterior", "Cod_Alumno", "NombreCompleto", "Tipo_Alumno", "Modalidad", "Cod_SitAcademica", "Matricula", "Fecha_Registro", "Mensaje_Error" },
            rows[0].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Equal(new[] { "OK", "Actualizado", "P1", "OLD", "NEW", "Ana Pérez López", "MBA", "PT", "ACTIVO", "AUDIT-M", "2026-10-01 00:00:00.000", "=SUM(1,2)<&\"" },
            rows[1].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Empty(sheet.Descendants(ns + "f"));
        Assert.All(sheet.Descendants(ns + "c"), cell => Assert.Equal("inlineStr", cell.Attribute("t")!.Value));
    }

    [Fact]
    public async Task State_selection_is_applied_to_the_download()
    {
        var (factory, tokens) = await Create();
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Permission));
        using var response = await client.GetAsync(Route + "/download?state=ERR&from=2026-10-01&to=2026-10-01");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync()); using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal("ERR", rows[1].Descendants(ns + "t").First().Value);
    }

    [Theory]
    [InlineData("?state=OK&from=2000-01-01&to=2000-01-01", "No hay registros para exportar.")]
    [InlineData("?state=BAD&from=2026-10-01&to=2026-10-01", "El estado no es válido.")]
    public async Task Empty_or_unknown_state_returns_message_without_attachment(string query, string message)
    {
        var (factory, tokens) = await Create();
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Permission));
        using var response = await client.GetAsync(Route + "/download" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Theory]
    [InlineData("/filters")]
    [InlineData("/download" + Query)]
    public async Task Unavailable_storage_is_sanitized(string suffix)
    {
        var (factory, tokens) = await Create(unavailable: true);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Permission));
        using var response = await client.GetAsync(Route + suffix);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("El informe no está disponible.", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("database provider", await response.Content.ReadAsStringAsync());
    }
}
