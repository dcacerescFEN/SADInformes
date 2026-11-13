namespace Application.Common.Settings;

/// <summary>Estado de entrega de correo cargado desde AppConfig, con el formato de SADAlumnos.</summary>
public static class EmailDeliverySettings
{
    private sealed record Settings(bool IsEnabled, bool IsSmtpConfigured, string Recipient, string Sender);

    private static Settings? _settings;

    private static Settings Current =>
        Volatile.Read(ref _settings)
        ?? throw new InvalidOperationException("La configuración de correo no fue inicializada.");

    public static bool IsEnabled => Current.IsEnabled;
    public static bool IsSmtpConfigured => Current.IsSmtpConfigured;
    public static string Recipient => Current.Recipient;
    public static string Sender => Current.Sender;

    public static void Initialize(bool isEnabled, bool isSmtpConfigured, string recipient, string sender) =>
        Volatile.Write(ref _settings, new Settings(isEnabled, isSmtpConfigured, recipient, sender));
}
