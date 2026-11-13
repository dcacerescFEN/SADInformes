namespace Infrastructure.Configuration;

/// <summary>Configuración SMTP con el formato de SADAlumnos.</summary>
public static class EmailSettings
{
    public static readonly string[] Keys =
    [
        "SendEmail", "EmailFrom", "EmailTo", "EmailServer", "EmailPort",
    ];

    private sealed record Settings(bool SendEmail, string EmailFrom, string EmailTo, string Server, int Port);

    private static Settings? _settings;

    private static Settings Current =>
        Volatile.Read(ref _settings)
        ?? throw new InvalidOperationException("La configuración SMTP no fue inicializada.");

    public static bool SendEmail => Current.SendEmail;
    public static string EmailFrom => Current.EmailFrom;
    public static string EmailTo => Current.EmailTo;
    public static string Server => Current.Server;
    public static int Port => Current.Port;

    public static void Initialize(bool sendEmail, string emailFrom, string emailTo, string server, int port) =>
        Volatile.Write(ref _settings, new Settings(sendEmail, emailFrom, emailTo, server, port));
}
