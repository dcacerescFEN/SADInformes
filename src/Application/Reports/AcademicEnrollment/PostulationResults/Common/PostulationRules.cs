using System.Globalization;

namespace Application.Reports.AcademicEnrollment.PostulationResults.Common;

internal static class PostulationRules
{
    public static string Origin(string? value)
    {
        var origin = value?.Trim().ToUpperInvariant();
        return origin is "INSCRIPCION" or "AGREGA"
            ? origin
            : throw new ArgumentException("El origen no es válido.");
    }

    public static string Period(string? value)
    {
        var period = value?.Trim();
        return !string.IsNullOrEmpty(period) && period.Length <= 5
            ? period
            : throw new ArgumentException("El período no es válido.");
    }

    public static DateTime Date(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new ArgumentException($"La fecha {field} no es válida.");
        return date;
    }

    public static string FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture) ?? "";
}
