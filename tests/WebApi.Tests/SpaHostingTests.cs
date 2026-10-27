using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebApi.Tests;

public sealed class SpaHostingTests : IDisposable
{
    private const string Index = "<!doctype html><title>SADInformes fixture</title><app-root></app-root>";
    private readonly string _webRoot = Path.Combine(AppContext.BaseDirectory, "spa-" + Guid.NewGuid());
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SpaHostingTests()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), Index);
        File.WriteAllText(Path.Combine(_webRoot, "app.js"), "globalThis.spaProbe = true;");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseWebRoot(_webRoot);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
        });
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/postgrado/registro-tesis")]
    [InlineData("/apiculture")]
    public async Task Production_serves_the_SPA_for_client_routes(string route)
    {
        var response = await _client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Index, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Production_serves_static_assets()
    {
        var response = await _client.GetAsync("/app.js");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType!.ToString());
        Assert.Equal("globalThis.spaProbe = true;", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/unknown")]
    [InlineData("/API/unknown")]
    [InlineData("/api/auth/unknown")]
    [InlineData("/scalar")]
    [InlineData("/scalar/")]
    [InlineData("/scalar/v1")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/missing.js")]
    public async Task Reserved_backend_paths_and_missing_assets_never_return_the_SPA(string route)
    {
        var response = await _client.GetAsync(route);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("app-root", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Hosting_preserves_existing_API_statuses()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/config/site")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await _client.PostAsync("/api/health", null)).StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        Directory.Delete(_webRoot, recursive: true);
    }
}
