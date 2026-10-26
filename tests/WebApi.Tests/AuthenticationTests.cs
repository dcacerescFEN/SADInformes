using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace WebApi.Tests;

public sealed class AuthenticationTests : IDisposable
{
    private const string Issuer = "https://sso-dev.fen.uchile.cl/realms/FenUChile";
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationTests()
    {
        var key = new RsaSecurityKey(_rsa) { KeyId = "test-key" };
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var config = new OpenIdConnectConfiguration { Issuer = Issuer };
                    config.SigningKeys.Add(key);
                    options.ConfigurationManager =
                        new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
                }));
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Public_configuration_contains_only_the_three_public_values()
    {
        var response = await _client.GetAsync("/api/config/keycloak");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(new[] { "clientId", "realm", "url" },
            body.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal("https://sso-dev.fen.uchile.cl", body.GetProperty("url").GetString());
        Assert.Equal("FenUChile", body.GetProperty("realm").GetString());
        Assert.Equal("SADInformes", body.GetProperty("clientId").GetString());
    }

    [Fact]
    public async Task Anonymous_requests_cannot_access_the_session()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_valid_token_for_this_client_returns_the_username()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("usuario-prueba", body.GetProperty("username").GetString());
    }

    [Theory]
    [InlineData("audience")]
    [InlineData("issuer")]
    [InlineData("signature")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("unsigned")]
    [InlineData("malformed")]
    public async Task Invalid_tokens_never_return_identity(string defect)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(defect));
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Existing_health_and_unknown_endpoints_keep_their_behavior()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"ok\",\"application\":\"SADInformes\"}",
            await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/reports")).StatusCode);
    }

    private string CreateToken(string? defect = null)
    {
        if (defect == "malformed") return "invalid-token";
        using var otherRsa = RSA.Create(2048);
        var key = new RsaSecurityKey(defect == "signature" ? otherRsa : _rsa) { KeyId = "test-key" };
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: defect == "issuer" ? "https://other.invalid/realms/other" : Issuer,
            audience: defect == "audience" ? "SADAlumnos" : "SADInformes",
            claims: [new Claim("sub", "test-user"), new Claim("preferred_username", "usuario-prueba")],
            notBefore: defect == "future" ? now.AddMinutes(5) : now.AddMinutes(-10),
            expires: defect == "expired" ? now.AddMinutes(-5) : now.AddMinutes(10),
            signingCredentials: defect == "unsigned" ? null :
                new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _rsa.Dispose();
    }
}
