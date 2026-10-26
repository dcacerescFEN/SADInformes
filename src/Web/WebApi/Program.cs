using System.Security.Claims;
using WebApi.Authentication;
using WebApi.Endpoints;
using Application.Navigation;
using Infrastructure.Data;
using Infrastructure.Navigation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddScoped<ISiteMenuProvider>(_ => new AppConfigSiteMenuProvider(
    () => new AppConfigContext(new DbContextOptionsBuilder<AppConfigContext>()
        .UseSqlServer(builder.Configuration.GetConnectionString("AppConfig"),
            options => options.CommandTimeout(10)).Options),
    builder.Configuration["ConfigureSite:ApplicationName"] ?? "",
    builder.Configuration["ConfigureSite:EnvironmentName"] ?? ""));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", application = "SADInformes" }))
    .AllowAnonymous();
app.MapGet("/api/config/keycloak", (PublicKeycloakConfiguration configuration) => Results.Ok(configuration))
    .AllowAnonymous();
app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new { username = user.Identity?.Name }))
    .RequireAuthorization();

app.MapSiteConfiguration();

app.Run();

public partial class Program;
