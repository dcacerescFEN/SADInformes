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

public sealed class SadSigmaEndpointTests
{
    private const string Route = "/api/reports/postgraduate/sad-sigma";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_SIGMA";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(
        SadSigmaTests.Procedures procedures, ISigmaRemuneracionesContext? sigma = null)
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
                if (sigma is not null) services.AddSingleton(sigma);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    private static ISigmaRemuneracionesContext HappySigma() => SadSigmaTests.CreateSigma();

    [Fact]
    public async Task Filters_and_report_are_not_cacheable_and_enrich_sigma()
    {
        var (factory, tokens) = Create(new SadSigmaTests.Procedures(), HappySigma());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var filters = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.True(filters.Headers.CacheControl!.NoStore);
        using var report = await client.GetAsync(Route + "?year=2024");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True(report.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        var row = json.RootElement.GetProperty("data").GetProperty("rows")[0];
        Assert.Equal("SI", row.GetProperty("existsInSigma").GetString());
        Assert.Equal("01/03/2024 10:30:00", row.GetProperty("createdInSigma").GetString());
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES", 403)]
    public async Task All_routes_require_the_report_permission(string? role, int status)
    {
        var procedures = new SadSigmaTests.Procedures();
        var (factory, tokens) = Create(procedures, HappySigma());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters", "?year=2024", "/download?year=2024" })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Download_has_thirty_eight_columns_with_sigma_created_last()
    {
        var (factory, tokens) = Create(new SadSigmaTests.Procedures(), HappySigma());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ReporteVistaSadSigma.xlsx", response.Content.Headers.ContentDisposition!.ToString());
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(xml); var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(2, rows.Length);
        var headers = rows[0].Descendants(ns + "t").Select(cell => cell.Value).ToArray();
        Assert.Equal(38, headers.Length);
        Assert.Equal("Existe_En_Sigma", headers[32]);
        Assert.Equal("Creado_En_Sigma", headers[37]);
        var values = rows[1].Descendants(ns + "t").Select(cell => cell.Value).ToArray();
        Assert.Equal("SI", values[32]); Assert.Equal("01/03/2024 10:30:00", values[37]);
        Assert.All(sheet.Descendants(ns + "c"), cell => Assert.Equal("inlineStr", cell.Attribute("t")!.Value));
        Assert.All(rows, row => Assert.Equal(38, row.Elements(ns + "c").Count()));
    }

    [Fact]
    public async Task Invalid_year_and_empty_export_are_rejected()
    {
        var procedures = new SadSigmaTests.Procedures();
        var (factory, tokens) = Create(procedures, HappySigma());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal((int)HttpStatusCode.BadRequest, (int)(await client.GetAsync(Route + "?year=Todos")).StatusCode);
        Assert.Null(procedures.Arguments);
        var (emptyFactory, emptyTokens) = Create(new SadSigmaTests.Procedures { Empty = true }, HappySigma());
        using var emptyHost = emptyFactory; using var emptySigning = emptyTokens; using var emptyClient = emptyFactory.CreateClient();
        emptyClient.SetBearer(emptyTokens.CreateToken("SADInformes", View));
        using var response = await emptyClient.GetAsync(Route + "/download?year=2024");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Procedure_errors_are_sanitized()
    {
        var (factory, tokens) = Create(new SadSigmaTests.Procedures { Fail = true }, HappySigma());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "?year=2024");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }
}
