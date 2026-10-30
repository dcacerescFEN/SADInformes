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

public sealed class ThesisDefensesEndpointTests
{
    private const string Route = "/api/reports/postgraduate/thesis-defenses";
    private const string Query = "?defenseYear=Todos&studentType=MBA&modality=Todos&entryYear=Todos";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_THESIS_DEFENSES";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(ThesisDefensesTests.Procedures procedures)
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
    public async Task Filters_are_not_cacheable_and_reload_modalities_for_type()
    {
        var procedures = new ThesisDefensesTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/filters?studentType=MBA");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Contains("Todos", data.GetProperty("studentTypes").EnumerateArray().Select(row => row.GetString()));
        Assert.Equal(new[] { "Todos", "PT", "FT" }, data.GetProperty("modalities").EnumerateArray().Select(row => row.GetString()));
        Assert.Equal("MBA", procedures.ModalityType);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_SIGMA", 403)]
    public async Task All_routes_require_the_report_permission(string? role, int status)
    {
        var (factory, tokens) = Create(new ThesisDefensesTests.Procedures());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters?studentType=MBA", Query,
                     "/download/coordination" + Query, "/download/treasury" + Query })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
    }

    [Fact]
    public async Task Report_returns_both_distinct_shapes()
    {
        var (factory, tokens) = Create(new ThesisDefensesTests.Procedures());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetProperty("coordinationTotal").GetInt32());
        Assert.Equal(1, data.GetProperty("treasuryTotal").GetInt32());
        Assert.Equal(20, data.GetProperty("coordination")[0].EnumerateObject().Count());
        Assert.Equal(17, data.GetProperty("treasury")[0].EnumerateObject().Count());
    }

    [Fact]
    public async Task Both_downloads_have_their_legacy_headers_and_sheet_shapes()
    {
        var (factory, tokens) = Create(new ThesisDefensesTests.Procedures());
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var coordination = await client.GetAsync(Route + "/download/coordination" + Query);
        Assert.Equal(HttpStatusCode.OK, coordination.StatusCode);
        Assert.Contains("ReporteComisionesCoordinacion-", coordination.Content.Headers.ContentDisposition!.ToString());
        using var coordinationBytes = new MemoryStream(await coordination.Content.ReadAsByteArrayAsync());
        using var coordinationZip = new ZipArchive(coordinationBytes, ZipArchiveMode.Read);
        using var coordinationXml = coordinationZip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var coordinationSheet = XDocument.Load(coordinationXml);
        var coordinationRows = coordinationSheet.Descendants(ns + "row").ToArray();
        Assert.Equal(20, coordinationRows[0].Descendants(ns + "t").Count());
        Assert.Equal("Tema/Nombre Tésis", coordinationRows[0].Descendants(ns + "t").ElementAt(11).Value);
        Assert.Equal(20, coordinationRows[1].Elements(ns + "c").Count());

        using var treasury = await client.GetAsync(Route + "/download/treasury" + Query);
        Assert.Equal(HttpStatusCode.OK, treasury.StatusCode);
        Assert.Contains("ReporteComisionesTesoreria-", treasury.Content.Headers.ContentDisposition!.ToString());
        using var treasuryBytes = new MemoryStream(await treasury.Content.ReadAsByteArrayAsync());
        using var treasuryZip = new ZipArchive(treasuryBytes, ZipArchiveMode.Read);
        using var treasuryXml = treasuryZip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var treasurySheet = XDocument.Load(treasuryXml);
        var treasuryRows = treasurySheet.Descendants(ns + "row").ToArray();
        Assert.Equal(17, treasuryRows[0].Descendants(ns + "t").Count());
        Assert.Equal(17, treasuryRows[1].Elements(ns + "c").Count());
    }

    [Fact]
    public async Task Invalid_selection_and_empty_exports_are_rejected()
    {
        var procedures = new ThesisDefensesTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal((int)HttpStatusCode.BadRequest,
            (int)(await client.GetAsync(Route + "?defenseYear=202&studentType=MBA&modality=Todos&entryYear=Todos")).StatusCode);
        Assert.Equal(0, procedures.ReportCalls);

        var (emptyFactory, emptyTokens) = Create(new ThesisDefensesTests.Procedures { Empty = true });
        using var emptyHost = emptyFactory; using var emptySigning = emptyTokens; using var emptyClient = emptyFactory.CreateClient();
        emptyClient.SetBearer(emptyTokens.CreateToken("SADInformes", View));
        using var response = await emptyClient.GetAsync(Route + "/download/coordination" + Query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Procedure_errors_are_sanitized()
    {
        var (factory, tokens) = Create(new ThesisDefensesTests.Procedures { Fail = true });
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }
}
