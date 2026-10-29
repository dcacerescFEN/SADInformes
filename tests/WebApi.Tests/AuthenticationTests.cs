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
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Application.Navigation;
using Infrastructure.Data;
using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Configuration = Domain.Entities.AppConfig.Configuration;
using WebApi.Authentication;
using Application.Common.Mediator;
using Application.Config.Queries.GetSiteConfig;
using Domain.Common;

namespace WebApi.Tests;

public sealed class AuthenticationTests : IDisposable
{
    private const string Issuer = "https://sso-dev.fen.uchile.cl/realms/FenUChile";
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _clientId;

    public AuthenticationTests()
    {
        var key = new RsaSecurityKey(_rsa) { KeyId = "test-key" };
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConfigureSite:ApplicationName"] = "SADInformes",
                ["ConfigureSite:EnvironmentName"] = "DEV",
                ["ConnectionStrings:AppConfig"] = "",
            }));
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var config = new OpenIdConnectConfiguration { Issuer = Issuer };
                    config.SigningKeys.Add(key);
                    options.ConfigurationManager =
                        new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
                });
            });
        });
        _client = _factory.CreateClient();
        _clientId = _factory.Services.GetRequiredService<PublicKeycloakConfiguration>().ClientId;
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
        Assert.Contains(_clientId, new[] { "SADInformes", "SADInformes-iis" });
        Assert.Equal(_clientId, body.GetProperty("clientId").GetString());
    }

    [Fact]
    public async Task Site_dispatches_its_query_while_public_Keycloak_reads_configuration_directly()
    {
        var dispatcher = new RecordingDispatcher();
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IRequestDispatcher>(dispatcher)));
        using var client = factory.CreateClient();
        var publicResponse = await client.GetAsync("/api/config/keycloak");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        using var publicBody = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync());
        Assert.Equal(_clientId, publicBody.RootElement.GetProperty("clientId").GetString());
        Assert.Empty(dispatcher.Requests);
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var siteResponse = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, siteResponse.StatusCode);
        using var siteBody = JsonDocument.Parse(await siteResponse.Content.ReadAsStringAsync());
        Assert.True(siteBody.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("from-dispatcher", siteBody.RootElement.GetProperty("data").GetProperty("reportsMenuClientId").GetString());
        Assert.Equal(new[] { typeof(GetSiteConfig) }, dispatcher.Requests);
    }

    public sealed class RecordingDispatcher : IRequestDispatcher
    {
        public List<Type> Requests { get; } = [];
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request.GetType());
            object response = new ApiResponse<SiteConfigDTO>
            {
                Success = true,
                Data = new SiteConfigDTO { ReportsMenuClientId = "from-dispatcher" },
            };
            return Task.FromResult((TResponse)response);
        }
    }

    [Fact]
    public async Task Site_uses_the_shared_envelope_and_canonical_keys_override_legacy_aliases()
    {
        using var factory = MenuFactory([
            MenuRow("StudentUrl", "https://legacy.example.test"),
            MenuRow("StudentsMenuUrl", "https://canonical.example.test"),
            MenuRow("StudentsMenuClientId", "SADAlumnos-iis"),
        ]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.True(body.RootElement.TryGetProperty("message", out _));
        var data = body.RootElement.GetProperty("data");
        Assert.Equal(18, data.EnumerateObject().Count());
        Assert.Equal("https://canonical.example.test", data.GetProperty("studentsMenuUrl").GetString());
        Assert.Equal("SADAlumnos-iis", data.GetProperty("studentsMenuClientId").GetString());
        Assert.False(body.RootElement.TryGetProperty("menu", out _));
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

    [Theory]
    [InlineData("SADInformes", "SADInformes", "PERMISSION_REPORT_ACCESS", true)]
    [InlineData("SADInformes", "SADInformes", "ROLE_REPORT_ADMIN", true)]
    [InlineData("SADInformes", "SADInformes", "ROLE_REPORT_SUPERADMIN", true)]
    [InlineData("SADInformes-iis", "SADInformes-iis", "PERMISSION_REPORT_ACCESS", true)]
    [InlineData("SADInformes-iis", "SADInformes-iis", "ROLE_REPORT_ADMIN", true)]
    [InlineData("SADInformes-iis", "SADInformes-iis", "ROLE_REPORT_SUPERADMIN", true)]
    [InlineData("SADInformes", "SADAlumnos", "PERMISSION_REPORT_ACCESS", false)]
    [InlineData("SADInformes", "SADInformes-iis", "PERMISSION_REPORT_ACCESS", false)]
    [InlineData("SADInformes-iis", "SADInformes", "PERMISSION_REPORT_ACCESS", false)]
    [InlineData("SADInformes", "SADInformes", "REPORT_ACCESS", false)]
    [InlineData("SADInformes", "SADInformes", "ROLE_STUDENT_SUPERADMIN", false)]
    [InlineData("SADInformes", "SADInformes", "ROLE_REPORT_ADMIN_EXTRA", false)]
    [InlineData("SADInformes", "SADInformes", "PERMISSION_REPORT_ACCESS_EXTRA", false)]
    public async Task Report_access_accepts_only_exact_roles_from_the_configured_client(
        string clientId, string roleClient, string role, bool allowed)
    {
        using var configured = MenuFactory([]);
        using var factory = configured.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(new PublicKeycloakConfiguration("https://sso-dev.fen.uchile.cl", "FenUChile", clientId));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Audience = clientId;
                options.TokenValidationParameters.ValidAudience = clientId;
            });
        }));
        using var client = factory.CreateClient();
        var resourceAccess = JsonSerializer.Serialize(new Dictionary<string, object>
            { [roleClient] = new { roles = new[] { role } } });
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(resourceAccess: resourceAccess, clientId: clientId));
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        if (!allowed) Assert.Equal("", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"not-json\"")]
    [InlineData("{\"SADInformes\":{\"roles\":\"PERMISSION_REPORT_ACCESS\"}}")]
    [InlineData("{\"SADInformes\":{\"roles\":[1,null]}}")]
    public async Task Report_access_rejects_missing_or_malformed_roles_without_data(string access)
    {
        using var factory = MenuFactory([]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(resourceAccess: access));
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/config/keycloak")).StatusCode);
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
        var menu = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        Assert.Equal(18, menu.EnumerateObject().Count());
        Assert.Equal("https://alumnos.example.test", menu.GetProperty("studentsMenuUrl").GetString());
        Assert.Equal("SADAlumnos", menu.GetProperty("studentsMenuClientId").GetString());
        Assert.Equal("https://sadprofesores-dev.fen.uchile.cl", menu.GetProperty("teachersMenuUrl").GetString());
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
            services.AddScoped<AppConfigContext>(
                _ => throw new InvalidOperationException("private-database-detail"))));
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
    public async Task Missing_menu_values_use_reference_defaults_and_the_confirmed_Reports_destination()
    {
        using var factory = MenuFactory([]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var menu = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        var defaults = new (string Key, string Url, string Client)[]
        {
            ("studentsMenu", "https://sadalumnos-dev.fen.uchile.cl", "SADAlumnos"),
            ("teachersMenu", "https://sadprofesores-dev.fen.uchile.cl", "SADProfesores"),
            ("coursesMenu", "https://sadcursos-dev.fen.uchile.cl", "SADCursos"),
            ("examsMenu", "https://sadexamenes-dev.fen.uchile.cl", "SADExamenes"),
            ("activitiesMenu", "https://sadactividades-dev.fen.uchile.cl", "SADActividades"),
            ("assistantsMenu", "https://sadayudantes-dev.fen.uchile.cl", "SADAyudantes"),
            ("processesMenu", "https://sadprocesos-dev.fen.uchile.cl", "SADProcesos"),
            ("reportsMenu", "https://sadinformes-dev.fen.uchile.cl", "SADInformes"),
            ("administrationMenu", "https://sadadministracion-dev.fen.uchile.cl", "SADAdministracion"),
        };
        Assert.Equal(18, menu.EnumerateObject().Count());
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
            var menu = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            Assert.Equal(url.Trim(), menu.GetProperty("studentsMenuUrl").GetString());
            Assert.Equal(clientId, menu.GetProperty("studentsMenuClientId").GetString());
        }
        else
        {
            Assert.DoesNotContain("custom.example.test", await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        }
    }

    [Theory]
    [InlineData("https://custom-reports.example.test", "SADInformes-iis")]
    [InlineData("  https://custom-reports.example.test  ", "")]
    public async Task Reports_values_from_AppConfig_override_defaults_without_changing_other_destinations(
        string url, string clientId)
    {
        using var factory = MenuFactory([
            MenuRow("reportsmenuurl", url), MenuRow("REPORTSMENUCLIENTID", clientId),
        ]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var menu = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        Assert.Equal(url.Trim(), menu.GetProperty("reportsMenuUrl").GetString());
        Assert.Equal(clientId, menu.GetProperty("reportsMenuClientId").GetString());
        Assert.Equal("SADAdministracion", menu.GetProperty("administrationMenuClientId").GetString());
        Assert.Equal("https://sadadministracion-dev.fen.uchile.cl", menu.GetProperty("administrationMenuUrl").GetString());
    }

    [Fact]
    public async Task Postgraduate_user_is_not_exposed_in_the_public_site_configuration()
    {
        using var factory = MenuFactory([MenuRow("PostgraduateUser", "private-report-user")]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var response = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-report-user", body);
        Assert.Equal(18, JsonDocument.Parse(body).RootElement.GetProperty("data").EnumerateObject().Count());
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
        var firstMenu = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        Assert.Equal("https://initial.example.test", firstMenu.GetProperty("studentsMenuUrl").GetString());
        using (var externalConfiguration = new AppConfigContext(options))
        {
            var row = await externalConfiguration.Configurations.SingleAsync();
            row.ConfigurationValue = "https://changed.example.test";
            await externalConfiguration.SaveChangesAsync();
        }
        var second = await client.GetAsync("/api/config/site");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstMenu.GetRawText(),
            JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetRawText());
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
            services.AddScoped(_ => new AppConfigContext(options))));
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

    private string CreateToken(string? defect = null, string? resourceAccess = null, string? clientId = null)
    {
        if (defect == "malformed") return "invalid-token";
        using var otherRsa = RSA.Create(2048);
        var key = new RsaSecurityKey(defect == "signature" ? otherRsa : _rsa) { KeyId = "test-key" };
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: defect == "issuer" ? "https://other.invalid/realms/other" : Issuer,
            audience: defect == "audience" ? "SADAlumnos" : clientId ?? _clientId,
            claims: [new Claim("sub", "test-user"), new Claim("preferred_username", "usuario-prueba"),
                new Claim("resource_access", resourceAccess ?? JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    [_clientId] = new { roles = new[] { "PERMISSION_REPORT_ACCESS" } },
                }), JsonClaimValueTypes.Json)],
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
