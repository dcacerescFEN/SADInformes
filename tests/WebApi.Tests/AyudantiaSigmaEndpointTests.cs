using System.IO.Compression;
using Infrastructure.Data;
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

public sealed class AyudantiaSigmaEndpointTests
{
    private const string Route = "/api/reports/postgraduate/ayudantias-sigma";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_SIGMA_TUTORSHIPS";

    private static (WebApplicationFactory<Program>, TokenFactory, SadContext Sad, SigmaRemuneracionesContext Sigma)
        Create(AyudantiaSigmaTests.Procedures procedures)
    {
        var (sad, sigma) = AyudantiaSigmaTests.CreateContexts();
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
                services.AddSingleton<ISigmaRemuneracionesContext>(sigma);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens, sad, sigma);
    }

    [Fact]
    public async Task Filters_and_report_are_not_cacheable_and_map_sigma()
    {
        var (factory, tokens, sad, sigma) = Create(new AyudantiaSigmaTests.Procedures());
        using var host = factory; using var signing = tokens; using var sadContext = sad; using var sigmaContext = sigma;
        using var client = factory.CreateClient();
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
        Assert.Equal("PASO 2", row.GetProperty("step").GetString());
        Assert.Equal("🚩", row.GetProperty("flag").GetString());
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_SIGMA", 403)]
    public async Task All_routes_require_the_report_permission(string? role, int status)
    {
        var (factory, tokens, sad, sigma) = Create(new AyudantiaSigmaTests.Procedures());
        using var host = factory; using var signing = tokens; using var sadContext = sad; using var sigmaContext = sigma;
        using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "/filters", "?year=2024", "/download?year=2024" })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
    }

    [Fact]
    public async Task Download_has_twenty_five_columns_with_sigma_fields_last()
    {
        var (factory, tokens, sad, sigma) = Create(new AyudantiaSigmaTests.Procedures());
        using var host = factory; using var signing = tokens; using var sadContext = sad; using var sigmaContext = sigma;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ReporteAyudantiaSigma-2024.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(xml);
        var rows = sheet.Descendants(ns + "row").ToArray();
        var headers = rows[0].Descendants(ns + "t").Select(cell => cell.Value).ToArray();
        Assert.Equal(25, headers.Length);
        Assert.Equal("Existe sigma", headers[20]);
        Assert.Equal("id remu", headers[24]);
        var values = rows[1].Descendants(ns + "t").Select(cell => cell.Value).ToArray();
        Assert.Equal("SI", values[20]);
        Assert.Equal("10", values[24]);
    }

    [Fact]
    public async Task Invalid_year_is_rejected()
    {
        var (factory, tokens, sad, sigma) = Create(new AyudantiaSigmaTests.Procedures());
        using var host = factory; using var signing = tokens; using var sadContext = sad; using var sigmaContext = sigma;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "?year=Todos");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
