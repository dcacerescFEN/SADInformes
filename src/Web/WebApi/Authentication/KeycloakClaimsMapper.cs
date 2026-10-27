using System.Security.Claims;
using System.Text.Json;
using Domain.Constants;

namespace WebApi.Authentication;

public static class KeycloakClaimsMapper
{
    public static bool HasPermissionOrGlobalAccess(ClaimsPrincipal user, string permission, string clientId)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        var resourceAccess = user.FindFirst("resource_access")?.Value;
        if (string.IsNullOrWhiteSpace(resourceAccess)) return false;
        try
        {
            using var document = JsonDocument.Parse(resourceAccess);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(clientId, out var client) || client.ValueKind != JsonValueKind.Object
                || !client.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
                return false;
            return roles.EnumerateArray().Any(role => role.ValueKind == JsonValueKind.String
                && (role.GetString() == Roles.Administrator || role.GetString() == Roles.SuperAdmin
                    || role.GetString() == "PERMISSION_" + permission));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
