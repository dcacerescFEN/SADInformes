using System.Globalization;

namespace Application.Reports.Regulation.Ranking.Common;

internal static class RankingRules
{
    private static readonly string[] Groups =
        ["ICMA", "ICME", "IICG", "CA", "LICADM", "LICECO", "LICINF"];
    private static readonly string[] Selections = ["PROMOCION", "INGRESO"];
    private static readonly string[] SortColumns = ["RANKING", "NOMBRE"];
    private static readonly string[] Directions = ["ASC", "DESC"];

    public static string Group(string? value) =>
        Normalize(value, Groups, "El grupo no es válido.");

    public static string Period(string? value)
    {
        var period = value?.Trim() ?? "";
        return period.Length is > 0 and <= 5
            ? period
            : throw new ArgumentException("El período no es válido.");
    }

    public static string Selection(string? value) =>
        Normalize(value, Selections, "El tipo de selección no es válido.");

    public static string SortBy(string? value) =>
        Normalize(value, SortColumns, "El orden no es válido.");

    public static string Direction(string? value) =>
        Normalize(value, Directions, "La dirección no es válida.");

    public static string Average(decimal? value) => value.HasValue
        ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
        : "";

    private static string Normalize(string? value, string[] allowed, string message)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? "";
        return allowed.Contains(normalized) ? normalized : throw new ArgumentException(message);
    }
}
