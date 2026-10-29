using Application.Common.Reporting;
using Infrastructure.Reporting;

namespace Infrastructure.Configuration;

// Instance-scoped settings: never share credentials across hosts or publish them to site config.
public sealed class ReportingSettings
{
    public static readonly string[] Keys =
    [
        "ReportingServiceBaseUrl", "ReportingServicePath", "ReportingServiceDomain",
        "ReportingServiceUsername", "ReportingServicePassword",
    ];
    private sealed record Snapshot(string BaseUrl, string Path, string Domain, string Username, string Password);
    private Snapshot? current;
    public ReportingSettings() : this(new Dictionary<string, string?>()) { }
    public ReportingSettings(IReadOnlyDictionary<string, string?> values) => Initialize(values);
    private Snapshot Current => Volatile.Read(ref current)
        ?? throw new InvalidOperationException("La configuración de reportes no está disponible.");
    public string BaseUrl => Current.BaseUrl;
    public string Path => Current.Path;
    public string Domain => Current.Domain;
    public string Username => Current.Username;
    public string Password => Current.Password;

    internal void Initialize(IReadOnlyDictionary<string, string?> values) =>
        Volatile.Write(ref current, Parse(values));

    private static Snapshot? Parse(IReadOnlyDictionary<string, string?> values)
    {
        // Same folder hierarchy as SADAlumnos, with this application's report folder.
        var baseUrl = (values.GetValueOrDefault(Keys[0]) ?? "https://reportes-pbi-dev2.fen.uchile.cl/ReportServer").Trim();
        var path = (values.GetValueOrDefault(Keys[1]) ?? "Reportes/Desarrollo/SADInformes").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
            return null;
        // Compatibility defaults explicitly requested to match SADAlumnos.
        // AppConfig always wins; explicit blanks fail closed rather than using a fallback.
        var domain = (values.GetValueOrDefault(Keys[2]) ?? "dfen").Trim();
        var username = (values.GetValueOrDefault(Keys[3]) ?? "reportes").Trim();
        var password = values.GetValueOrDefault(Keys[4]) ?? "RPpvXJAdMhseB5NV";
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;
        var candidate = new Snapshot(baseUrl, path, domain, username, password);
        try
        {
            _ = SsrsReportRequestBuilder.Build(new ReportRenderRequest("SolicitudesAlumnoPostgrado",
                new Dictionary<string, string>(), ReportFormat.PDF, TimeSpan.FromSeconds(60)),
                candidate.BaseUrl, candidate.Path);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException) { return null; }
        return candidate;
    }
}
