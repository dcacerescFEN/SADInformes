using System.Data.Common;
using Application.Configuration;
using Application.Navigation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

public static class InitialiserExtensions
{
    public static async Task InitialiseConfigurationAsync(this WebApplication app, IConfiguration configuration)
    {
        using var scope = app.Services.CreateScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<ConfigurationInitialiser>()
                .InitialiseAsync(configuration, app.Lifetime.ApplicationStopping);
        }
        catch (Exception error) when (error is DbException or InvalidOperationException or ArgumentException)
        {
            // Sin publicación parcial: el menú devuelve 503, pero health y sesión siguen disponibles.
        }
    }
}

public sealed class ConfigurationInitialiser(
    IAppConfigurationReader reader, SiteSettings settings, ILogger<ConfigurationInitialiser> logger)
{
    private static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>
    {
        ["StudentUrl"] = "https://sadalumnos-dev.fen.uchile.cl",
        ["TeacherUrl"] = "https://sadprofesores-dev.fen.uchile.cl",
        ["CoursesMenuUrl"] = "https://sadcursos-dev.fen.uchile.cl",
        ["ExamsMenuUrl"] = "https://sadexamenes-dev.fen.uchile.cl",
        ["ActivitiesMenuUrl"] = "https://sadactividades-dev.fen.uchile.cl",
        ["AssistantsMenuUrl"] = "https://sadayudantes-dev.fen.uchile.cl",
        ["ProcessesMenuUrl"] = "https://sadprocesos-dev.fen.uchile.cl",
        ["AdministrationMenuUrl"] = "https://sadadministracion-dev.fen.uchile.cl",
        ["StudentClientId"] = "SADAlumnos",
        ["TeacherClientId"] = "SADProfesores",
        ["CoursesMenuClientId"] = "SADCursos",
        ["ExamsMenuClientId"] = "SADExamenes",
        ["ActivitiesMenuClientId"] = "SADActividades",
        ["AssistantsMenuClientId"] = "SADAyudantes",
        ["ProcessesMenuClientId"] = "SADProcesos",
        ["AdministrationMenuClientId"] = "SADAdministracion",
    };

    public async Task InitialiseAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            var application = configuration["ConfigureSite:ApplicationName"];
            var environment = configuration["ConfigureSite:EnvironmentName"];
            if (string.IsNullOrWhiteSpace(application) || string.IsNullOrWhiteSpace(environment))
                throw new InvalidOperationException("Los selectores de AppConfig son obligatorios.");

            var values = await reader.LoadAsync(application, environment, cancellationToken);
            var menu = SiteMenuFields.Names.ToDictionary(
                name => char.ToLowerInvariant(name[0]) + name[1..],
                name => (string?)(name.EndsWith("Url", StringComparison.Ordinal)
                    ? RequiredValue(values, name, Defaults[name])
                    : values.GetValueOrDefault(name) ?? Defaults[name]));
            var statusSite = RequiredValue(values, "StatusSite", "1");
            cancellationToken.ThrowIfCancellationRequested();
            settings.Initialize(statusSite, application, environment, menu);
        }
        catch (Exception error) when (error is DbException or InvalidOperationException or ArgumentException)
        {
            logger.LogWarning("No se pudo inicializar la configuración de navegación desde AppConfig.");
            throw;
        }
    }

    private static string RequiredValue(IReadOnlyDictionary<string, string?> values, string key, string fallback)
    {
        var value = values.GetValueOrDefault(key) ?? fallback;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"La configuración '{key}' no es válida.");
        return value.Trim();
    }
}
