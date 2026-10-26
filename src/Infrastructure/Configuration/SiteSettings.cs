using System.Collections.ObjectModel;

namespace Infrastructure.Configuration;

public sealed class SiteSettings
{
    private sealed record Settings(string StatusSite, string ApplicationName,
        string EnvironmentName, IReadOnlyDictionary<string, string?> Menu);
    private Settings? _settings;
    private Settings Current => Volatile.Read(ref _settings)
        ?? throw new InvalidOperationException("La configuración operativa del sitio no fue inicializada.");

    public string StatusSite => Current.StatusSite;
    public string ApplicationName => Current.ApplicationName;
    public string EnvironmentName => Current.EnvironmentName;
    public IReadOnlyDictionary<string, string?> Menu => Current.Menu;

    public void Initialize(string statusSite, string applicationName, string environmentName,
        IReadOnlyDictionary<string, string?> menu)
    {
        var copy = new ReadOnlyDictionary<string, string?>(menu.ToDictionary(pair => pair.Key, pair => pair.Value));
        var candidate = new Settings(statusSite, applicationName, environmentName, copy);
        if (Interlocked.CompareExchange(ref _settings, candidate, null) is not null)
            throw new InvalidOperationException("La configuración operativa del sitio ya ha sido inicializada.");
    }
}
