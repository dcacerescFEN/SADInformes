using System.Security.Claims;
using WebApi.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKeycloakAuthentication(builder.Configuration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", application = "SADInformes" }))
    .AllowAnonymous();
app.MapGet("/api/config/keycloak", (PublicKeycloakConfiguration configuration) => Results.Ok(configuration))
    .AllowAnonymous();
app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new { username = user.Identity?.Name }))
    .RequireAuthorization();

app.Run();

public partial class Program;
