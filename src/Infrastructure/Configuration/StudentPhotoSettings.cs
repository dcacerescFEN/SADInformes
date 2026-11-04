using Application.Common.Interfaces;

namespace Infrastructure.Configuration;

public sealed class StudentPhotoSettings : IStudentPhotoSettings
{
    public const string ConfigurationKey = "StudentPhotoBaseUrl";
    public const string DefaultBaseUrl = "https://fotos.fen.uchile.cl/";

    private string? _baseUrl;

    public string BaseUrl => _baseUrl ?? DefaultBaseUrl;

    internal void Initialize(string? value)
    {
        var configured = string.IsNullOrWhiteSpace(value) ? DefaultBaseUrl : value.Trim();
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var url)
            || url.Scheme != Uri.UriSchemeHttps
            || !(url.Host.Equals("fen.uchile.cl", StringComparison.OrdinalIgnoreCase)
                || url.Host.EndsWith(".fen.uchile.cl", StringComparison.OrdinalIgnoreCase))
            || !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment))
            throw new InvalidOperationException(
                "La configuración 'StudentPhotoBaseUrl' debe ser una URL HTTPS institucional sin parámetros ni fragmentos.");
        _baseUrl = configured.TrimEnd('/') + "/";
    }
}
