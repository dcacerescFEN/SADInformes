using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebApi.Tests;

public sealed class ApiDocumentationTests
{
    private static WebApplicationFactory<Program> Factory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
        });

    [Fact]
    public async Task Development_exposes_Scalar_and_documents_only_protected_operations_as_secured()
    {
        using var factory = Factory("Development");
        using var client = factory.CreateClient();
        var scalar = await client.GetAsync("/scalar/v1");
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
        Assert.Contains("text/html", scalar.Content.Headers.ContentType!.ToString());
        Assert.Contains("SADInformes", await scalar.Content.ReadAsStringAsync());

        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("SADInformes API", root.GetProperty("info").GetProperty("title").GetString());
        var flow = root.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("Keycloak").GetProperty("flows").GetProperty("authorizationCode");
        Assert.EndsWith("/protocol/openid-connect/auth", flow.GetProperty("authorizationUrl").GetString());
        Assert.EndsWith("/protocol/openid-connect/token", flow.GetProperty("tokenUrl").GetString());
        Assert.True(flow.GetProperty("scopes").TryGetProperty("openid", out _));
        var paths = root.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/config/site").GetProperty("get").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/config/keycloak").GetProperty("get").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/health").GetProperty("get").TryGetProperty("security", out _));
    }

    [Theory]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task Other_environments_do_not_expose_documentation(string environment)
    {
        using var factory = Factory(environment);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/scalar/v1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
