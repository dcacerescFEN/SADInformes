using System.Data.Common;
using System.Globalization;
using System.Net.Mail;
using Application.Common.Settings;
using Application.Navigation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

public static class InitialiserExtensions
{
    public static async Task InitialiseConfigurationAsync(
        this WebApplication app,
        IConfiguration configuration
    )
    {
        using var scope = app.Services.CreateScope();
        try
        {
            await scope
                .ServiceProvider.GetRequiredService<ConfigurationInitialiser>()
                .InitialiseAsync(configuration, app.Lifetime.ApplicationStopping);
        }
        catch (Exception error)
            when (error is DbException or InvalidOperationException or ArgumentException)
        {
            // Sin publicación parcial: el menú devuelve 503, pero health y sesión siguen disponibles.
        }
    }
}

public sealed class ConfigurationInitialiser(
    AppConfigContext configContext,
    SiteSettings settings,
    PostgraduateSettings postgraduateSettings,
    StudentPhotoSettings studentPhotoSettings,
    ILogger<ConfigurationInitialiser> logger,
    ReportingSettings reportingSettings
)
{
    private static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<
        string,
        string
    >
    {
        ["StudentsMenuUrl"] = "https://sadalumnos-dev.fen.uchile.cl",
        ["TeachersMenuUrl"] = "https://sadprofesores-dev.fen.uchile.cl",
        ["CoursesMenuUrl"] = "https://sadcursos-dev.fen.uchile.cl",
        ["ExamsMenuUrl"] = "https://sadexamenes-dev.fen.uchile.cl",
        ["ActivitiesMenuUrl"] = "https://sadactividades-dev.fen.uchile.cl",
        ["AssistantsMenuUrl"] = "https://sadayudantes-dev.fen.uchile.cl",
        ["ProcessesMenuUrl"] = "https://sadprocesos-dev.fen.uchile.cl",
        ["ReportsMenuUrl"] = "https://sadinformes-dev.fen.uchile.cl",
        ["AdministrationMenuUrl"] = "https://sadadministracion-dev.fen.uchile.cl",
        ["StudentsMenuClientId"] = "SADAlumnos",
        ["TeachersMenuClientId"] = "SADProfesores",
        ["CoursesMenuClientId"] = "SADCursos",
        ["ExamsMenuClientId"] = "SADExamenes",
        ["ActivitiesMenuClientId"] = "SADActividades",
        ["AssistantsMenuClientId"] = "SADAyudantes",
        ["ProcessesMenuClientId"] = "SADProcesos",
        ["ReportsMenuClientId"] = "SADInformes",
        ["AdministrationMenuClientId"] = "SADAdministracion",
    };

    public async Task InitialiseAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var application = configuration["ConfigureSite:ApplicationName"];
            var environment = configuration["ConfigureSite:EnvironmentName"];
            if (string.IsNullOrWhiteSpace(application) || string.IsNullOrWhiteSpace(environment))
                throw new InvalidOperationException(
                    "Los selectores de AppConfig son obligatorios."
                );

            var keys = SiteMenuFields
                .Names.Concat(SiteMenuFields.LegacyAliases.Values)
                .Append("StatusSite")
                .Append(PostgraduateSettings.UserConfigurationKey)
                .Append(StudentPhotoSettings.ConfigurationKey)
                .Concat(ReportingSettings.Keys)
                .Concat(EmailSettings.Keys)
                .Select(name => name.ToLowerInvariant())
                .ToArray();
            var rows = await configContext
                .Configurations.AsNoTracking()
                .Where(row =>
                    row.IsActive
                    && row.Application.IsActive
                    && row.Environment.IsActive
                    && row.Application.ApplicationName == application
                    && row.Environment.EnvironmentName == environment
                    && keys.Contains(row.ConfigurationName.ToLower())
                )
                .Select(row => new { row.ConfigurationName, row.ConfigurationValue })
                .ToListAsync(cancellationToken);
            var values = rows.ToDictionary(
                row => row.ConfigurationName,
                row => (string?)row.ConfigurationValue,
                StringComparer.OrdinalIgnoreCase
            );
            var menu = SiteMenuFields.Names.ToDictionary(
                name => char.ToLowerInvariant(name[0]) + name[1..],
                name =>
                    (string?)(
                        name.EndsWith("Url", StringComparison.Ordinal)
                            ? RequiredValue(values, name, Defaults[name])
                            : ConfiguredValue(values, name) ?? Defaults[name]
                    )
            );
            var statusSite = RequiredValue(values, "StatusSite", "1");
            cancellationToken.ThrowIfCancellationRequested();
            settings.Initialize(statusSite, application, environment, menu);
            postgraduateSettings.Initialize(
                values.GetValueOrDefault(PostgraduateSettings.UserConfigurationKey)
            );
            studentPhotoSettings.Initialize(
                values.GetValueOrDefault(StudentPhotoSettings.ConfigurationKey)
            );
            reportingSettings.Initialize(values);
            var email = ParseEmailSettings(values);
            EmailSettings.Initialize(
                email.sendEmail,
                email.emailFrom,
                email.emailTo,
                email.server,
                email.port
            );
            EmailDeliverySettings.Initialize(
                email.sendEmail,
                !string.IsNullOrWhiteSpace(email.server),
                email.emailTo,
                email.emailFrom
            );
        }
        catch (Exception error)
            when (error is DbException or InvalidOperationException or ArgumentException)
        {
            logger.LogWarning(
                "No se pudo inicializar la configuración de navegación desde AppConfig."
            );
            throw;
        }
    }

    internal static (
        bool sendEmail,
        string emailFrom,
        string emailTo,
        string server,
        int port
    ) ParseEmailSettings(IReadOnlyDictionary<string, string?> values)
    {
        var sendEmail = RequiredFlag(values, "SendEmail", true);
        var emailFrom = RequiredValue(values, "EmailFrom", "secretariaestudios@fen.uchile.cl");
        var emailTo = RequiredValue(values, "EmailTo", "dcaceresc@fen.uchile.cl");
        var server = RequiredValue(values, "EmailServer", "smtp-mailhog-dev.fen.uchile.cl");
        var portValue = RequiredValue(values, "EmailPort", "1025");

        if (!MailAddress.TryCreate(emailFrom, out _))
            throw new InvalidOperationException("La configuración 'EmailFrom' no es válida.");
        if (!MailAddress.TryCreate(emailTo, out _))
            throw new InvalidOperationException("La configuración 'EmailTo' no es válida.");
        if (Uri.CheckHostName(server) == UriHostNameType.Unknown)
            throw new InvalidOperationException("La configuración 'EmailServer' no es válida.");
        if (
            !int.TryParse(portValue, NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            || port is < 1 or > 65535
        )
            throw new InvalidOperationException("La configuración 'EmailPort' no es válida.");

        return (sendEmail, emailFrom, emailTo, server, port);
    }

    private static bool RequiredFlag(
        IReadOnlyDictionary<string, string?> values,
        string key,
        bool fallback
    )
    {
        var value = ConfiguredValue(values, key);
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        return value.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ when bool.TryParse(value, out var parsed) => parsed,
            _ => throw new InvalidOperationException($"La configuración '{key}' no es válida."),
        };
    }

    private static string? ConfiguredValue(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (values.TryGetValue(key, out var value))
            return value;
        return SiteMenuFields.LegacyAliases.TryGetValue(key, out var alias)
            ? values.GetValueOrDefault(alias)
            : null;
    }

    private static string RequiredValue(
        IReadOnlyDictionary<string, string?> values,
        string key,
        string fallback
    )
    {
        var value = ConfiguredValue(values, key) ?? fallback;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"La configuración '{key}' no es válida.");
        return value.Trim();
    }
}
