using System.Globalization;

namespace Application.Reports.Regulation.CuadroHonor.Common;

internal static class HonorRollRules
{
    private static readonly string[] Careers = ["IC", "IICG", "CA"];

    public static string Career(string? value)
    {
        var career = value?.Trim().ToUpperInvariant() ?? "";
        return Careers.Contains(career) ? career : throw new ArgumentException("La carrera no es válida.");
    }

    public static string EntryYear(string? value) => Year(value, "El año de ingreso no es válido.");

    public static string HonorYear(string? value) => Year(value, "El año del cuadro de honor no es válido.");

    public static string Average(decimal? value) =>
        value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "";

    public static string Percentile(decimal? value) =>
        value.HasValue ? value.Value.ToString("0.####", CultureInfo.InvariantCulture) : "";

    private static string Year(string? value, string message)
    {
        var year = value?.Trim() ?? "";
        return year.Length == 4 && year.All(char.IsAsciiDigit) ? year : throw new ArgumentException(message);
    }
}
