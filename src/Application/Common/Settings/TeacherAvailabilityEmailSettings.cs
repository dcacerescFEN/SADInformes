namespace Application.Common.Settings;

/// <summary>Configuración de correo de disponibilidad horaria.</summary>
public static class TeacherAvailabilityEmailSettings
{
    public const string BccKey = "ReportAvailabilityEmailBcc";
    public const string DefaultBcc = "desarrollo@fen.uchile.cl";

    private static string _bcc = DefaultBcc;

    public static string Bcc => Volatile.Read(ref _bcc);

    public static void Initialize(string? value) =>
        Volatile.Write(ref _bcc, string.IsNullOrWhiteSpace(value) ? DefaultBcc : value.Trim());
}
