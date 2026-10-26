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
using Application.Navigation;
using Infrastructure.Data;
using Application.Configuration;
using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Configuration = Domain.Entities.AppConfig.Configuration;

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

    [Fact]
    public async Task The_menu_requires_authentication()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/config/site")).StatusCode);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("environment")]
    [InlineData("inactive-config")]
    [InlineData("inactive-application")]
    [InlineData("inactive-environment")]
    public async Task Menu_reads_only_active_SADInformes_DEV_rows_and_known_keys(string excluded)
    {
        var rows = new[]
        {
            MenuRow("StudentUrl", "https://alumnos.example.test"),
            MenuRow("studentclientid", "SADAlumnos"),
            MenuRow("ConnectionString", "never-publish-this"),
            MenuRow("TeacherUrl", "https://excluded.example.test", excluded),
        };
        using var factory = MenuFactory(rows);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var menu = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("menu");
        Assert.Equal(16, menu.EnumerateObject().Count());
        Assert.Equal("https://alumnos.example.test", menu.GetProperty("studentUrl").GetString());
        Assert.Equal("SADAlumnos", menu.GetProperty("studentClientId").GetString());
        Assert.Equal("https://sadprofesores-dev.fen.uchile.cl", menu.GetProperty("teacherUrl").GetString());
        Assert.DoesNotContain("never-publish-this", menu.GetRawText());
    }

    [Fact]
    public async Task Duplicate_active_keys_fail_closed_without_exposing_values()
    {
        using var factory = MenuFactory([
            MenuRow("StudentUrl", "https://first.example.test"),
            MenuRow("studenturl", "https://second.example.test"),
        ]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("example.test", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Database_errors_return_only_a_sanitized_unavailable_response()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IAppConfigurationReader>(new AppConfigConfigurationReader(
                () => throw new InvalidOperationException("private-database-detail")))));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-database-detail", body);
        Assert.Equal("La navegación no está disponible.",
            JsonDocument.Parse(body).RootElement.GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task Missing_menu_values_use_the_same_public_defaults_as_SADAlumnos()
    {
        using var factory = MenuFactory([]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var menu = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("menu");
        var defaults = new (string Key, string Url, string Client)[]
        {
            ("student", "https://sadalumnos-dev.fen.uchile.cl", "SADAlumnos"),
            ("teacher", "https://sadprofesores-dev.fen.uchile.cl", "SADProfesores"),
            ("coursesMenu", "https://sadcursos-dev.fen.uchile.cl", "SADCursos"),
            ("examsMenu", "https://sadexamenes-dev.fen.uchile.cl", "SADExamenes"),
            ("activitiesMenu", "https://sadactividades-dev.fen.uchile.cl", "SADActividades"),
            ("assistantsMenu", "https://sadayudantes-dev.fen.uchile.cl", "SADAyudantes"),
            ("processesMenu", "https://sadprocesos-dev.fen.uchile.cl", "SADProcesos"),
            ("administrationMenu", "https://sadadministracion-dev.fen.uchile.cl", "SADAdministracion"),
        };
        Assert.Equal(16, menu.EnumerateObject().Count());
        foreach (var (key, url, id) in defaults)
        {
            Assert.Equal(url, menu.GetProperty(key + "Url").GetString());
            Assert.Equal(id, menu.GetProperty(key + "ClientId").GetString());
        }
    }

    [Theory]
    [InlineData(" https://custom.example.test ", "SADAlumnos-iis", HttpStatusCode.OK)]
    [InlineData("https://custom.example.test", "", HttpStatusCode.OK)]
    [InlineData("   ", "SADAlumnos", HttpStatusCode.ServiceUnavailable)]
    public async Task Configured_values_override_defaults_and_blank_URLs_are_rejected(
        string url, string clientId, HttpStatusCode expected)
    {
        using var factory = MenuFactory([
            MenuRow("StudentUrl", url), MenuRow("StudentClientId", clientId),
        ]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            var menu = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("menu");
            Assert.Equal(url.Trim(), menu.GetProperty("studentUrl").GetString());
            Assert.Equal(clientId, menu.GetProperty("studentClientId").GetString());
        }
        else
        {
            Assert.DoesNotContain("custom.example.test", await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        }
    }

    [Fact]
    public async Task Configuration_is_published_once_at_startup_not_read_for_each_request()
    {
        var options = new DbContextOptionsBuilder<AppConfigContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var factory = MenuFactory([MenuRow("StudentUrl", "https://initial.example.test")], options);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var first = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstMenu = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement.GetProperty("menu");
        Assert.Equal("https://initial.example.test", firstMenu.GetProperty("studentUrl").GetString());
        using (var externalConfiguration = new AppConfigContext(options))
        {
            var row = await externalConfiguration.Configurations.SingleAsync();
            row.ConfigurationValue = "https://changed.example.test";
            await externalConfiguration.SaveChangesAsync();
        }
        var second = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstMenu.GetRawText(),
            JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement.GetProperty("menu").GetRawText());
    }

    private WebApplicationFactory<Program> MenuFactory(Configuration[] rows, DbContextOptions<AppConfigContext>? configuredOptions = null)
    {
        var options = configuredOptions ?? new DbContextOptionsBuilder<AppConfigContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var seed = new AppConfigContext(options))
        {
            seed.Configurations.AddRange(rows);
            seed.SaveChanges();
        }
        return _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IAppConfigurationReader>(new AppConfigConfigurationReader(
                () => new AppConfigContext(options)))));
    }

    private static Configuration MenuRow(string key, string value, string? excluded = null) => new()
    {
        ConfigurationName = key, ConfigurationValue = value, IsActive = excluded != "inactive-config",
        Application = new()
        {
            ApplicationName = excluded == "application" ? "SADAlumnos" : "SADInformes",
            IsActive = excluded != "inactive-application",
        },
        Environment = new()
        {
            EnvironmentName = excluded == "environment" ? "PROD" : "DEV",
            IsActive = excluded != "inactive-environment",
        },
    };

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
