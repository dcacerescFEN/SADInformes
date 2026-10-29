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

public sealed class WeightedRankingEndpointTests
{
    private const string Route = "/api/reports/postgraduate/weighted-ranking";
    private const string Query = "?studentType=MBA&modality=PT&cohort=2024&year=2026";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_WEIGHTED_RANKING";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(WeightedRankingTests.Procedures procedures)
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
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    [Fact]
    public async Task Authenticated_filter_catalog_cannot_be_cached()
    {
        var (factory, tokens) = Create(new WeightedRankingTests.Procedures());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/filters?studentType=MBA");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl.NoStore);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Todos", body);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES", 403)]
    public async Task All_routes_require_the_weighted_ranking_permission(string? role, int status)
    {
        var procedures = new WeightedRankingTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters", "", "/download" })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix + Query)).StatusCode);
        Assert.Null(procedures.Arguments);
        Assert.Null(procedures.ModalityType);
    }

    [Theory]
    [InlineData(View)]
    [InlineData("ROLE_REPORT_ADMIN")]
    [InlineData("ROLE_REPORT_SUPERADMIN")]
    public async Task Allowed_roles_can_read_catalogues_and_report_without_reranking(string role)
    {
        var (factory, tokens) = Create(new WeightedRankingTests.Procedures());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", role));

        using var filters = await client.GetAsync(Route + "/filters?studentType=MBA");
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.DoesNotContain("Todos", await filters.Content.ReadAsStringAsync());

        using var report = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        using var json = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var rows = json.RootElement.GetProperty("data").GetProperty("rows");
        Assert.Equal(new[] { 2, 1 }, rows.EnumerateArray().Select(row => row.GetProperty("position").GetInt32()));
        Assert.Equal("5.250", rows[0].GetProperty("weightedAverage").GetString());
        Assert.Equal("0.5000", rows[0].GetProperty("percentile").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("?studentType=Todos&modality=PT&cohort=2024&year=2026")]
    public async Task Invalid_inputs_fail_without_calling_ranking(string query)
    {
        var procedures = new WeightedRankingTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        foreach (var suffix in new[] { "", "/download" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + suffix + query)).StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Excel_has_eight_legacy_print_columns_without_photos_and_preserves_order_precision_and_formula_text()
    {
        var (factory, tokens) = Create(new WeightedRankingTests.Procedures());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("Ranking_Programas_Graduados.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(xml);
        var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(3, rows.Length);
        Assert.Equal(new[] { "Posición", "Nombre", "Rut", "Código Alumno", "Semestre de Ingreso", "Situación", "Promedio", "Percentil" },
            rows[0].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Equal(new[] { "2", "Ana Pérez", "00123456-K", "A/1", "2024A", "TITULADO", "5.250", "0.5000" },
            rows[1].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Equal("=FORMULA<>&", rows[2].Descendants(ns + "t").ElementAt(1).Value);
        Assert.Empty(sheet.Descendants(ns + "f"));
        Assert.All(sheet.Descendants(ns + "c"), cell => Assert.Equal("inlineStr", cell.Attribute("t")!.Value));
        Assert.All(rows, row => Assert.Equal(8, row.Elements(ns + "c").Count()));
    }

    [Fact]
    public async Task Empty_report_has_no_invented_rows_and_empty_export_has_no_attachment()
    {
        var (factory, tokens) = Create(new WeightedRankingTests.Procedures { Empty = true });
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var report = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Contains("\"total\":0", await report.Content.ReadAsStringAsync());
        using var export = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.BadRequest, export.StatusCode);
        Assert.Contains("Sin Datos", await export.Content.ReadAsStringAsync());
        Assert.Null(export.Content.Headers.ContentDisposition);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/download")]
    public async Task Procedure_errors_including_single_row_division_are_sanitized(string suffix)
    {
        var (factory, tokens) = Create(new WeightedRankingTests.Procedures { Fail = true });
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + suffix + Query);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }
}
