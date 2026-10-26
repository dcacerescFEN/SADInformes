using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Authentication;

public sealed record PublicKeycloakConfiguration(string Url, string Realm, string ClientId);

public static class KeycloakAuthentication
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration["Keycloak:Url"];
        var realm = configuration["Keycloak:Realm"];
        var clientId = configuration["Keycloak:ClientId"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(realm) || string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("La configuración pública de Keycloak es inválida.");

        var publicConfiguration = new PublicKeycloakConfiguration(url.TrimEnd('/'), realm, clientId);
        var authority = $"{publicConfiguration.Url}/realms/{realm}";
        services.AddSingleton(publicConfiguration);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = clientId;
            options.RequireHttpsMetadata = true;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                NameClaimType = "preferred_username",
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });
        services.AddAuthorization();
        return services;
    }
}
