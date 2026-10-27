using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WebApi.Authentication;

namespace WebApi.Tests;

/// <summary>
/// Emite tokens de prueba firmados localmente y configura la validación JWT equivalente
/// a Keycloak, sin usuarios ni servidor reales.
/// </summary>
public sealed class TokenFactory : IDisposable
{
    public const string Issuer = "https://sso-dev.fen.uchile.cl/realms/FenUChile";
    private readonly RSA _rsa = RSA.Create(2048);

    public void Configure(IServiceCollection services)
    {
        var key = new RsaSecurityKey(_rsa) { KeyId = "test-key" };
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            configuration.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        });
    }

    public void UseClient(IServiceCollection services, string clientId)
    {
        services.AddSingleton(new PublicKeycloakConfiguration("https://sso-dev.fen.uchile.cl", "FenUChile", clientId));
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Audience = clientId;
            options.TokenValidationParameters.ValidAudience = clientId;
        });
    }

    public string CreateToken(string clientId, params string[] roles)
    {
        var access = new Dictionary<string, object> { [clientId] = new { roles } };
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: clientId,
            claims:
            [
                new Claim("sub", "test-user"),
                new Claim("preferred_username", "usuario-prueba"),
                new Claim("resource_access", JsonSerializer.Serialize(access), JsonClaimValueTypes.Json),
            ],
            notBefore: now.AddMinutes(-10),
            expires: now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new RsaSecurityKey(_rsa) { KeyId = "test-key" }, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose() => _rsa.Dispose();
}

public static class TestClientExtensions
{
    public static void SetBearer(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
