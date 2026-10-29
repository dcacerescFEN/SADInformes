using Application.Common.Interfaces;
using Application.Common.Validation;
using Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRankingFilters;

namespace Application.Reports.Postgraduate.WeightedRanking;

internal static class WeightedRankingCatalog
{
    internal static string Required(string? value, string message, int maxLength)
    {
        var text = ConcreteFilter.Required(message, value);
        if (text.Length > maxLength || text.Any(char.IsControl)) throw new ArgumentException(message);
        return text;
    }

    internal static bool IsYear(string? value) =>
        value is { Length: 4 } && value.All(char.IsAsciiDigit) && value != "0000";

    private static bool IsConcrete(string? value)
    {
        try { _ = Required(value, "Filtro inválido.", 30); return true; }
        catch (ArgumentException) { return false; }
    }

    internal static async Task<WeightedRankingFiltersDTO> ReadAsync(
        ISadContextProcedures procedures, string? type, CancellationToken ct)
    {
        var selected = string.IsNullOrWhiteSpace(type)
            ? null
            : Required(type, "Seleccione un tipo de alumno.", 30);
        var types = (await procedures.SpRec_TiposAlumnoAsync("NO", "", cancellationToken: ct) ?? [])
            .Where(row => IsConcrete(row.Tipo))
            .Select(row => new WeightedRankingOptionDTO(
                row.Tipo.Trim(), row.Nombre?.Trim() ?? row.Tipo.Trim()))
            .DistinctBy(row => row.Value)
            .ToArray();
        selected ??= types.FirstOrDefault()?.Value;
        if (selected is not null && !types.Any(row => row.Value == selected))
            throw new ArgumentException("El tipo de alumno no es válido.");

        var years = (await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(IsYear).Cast<string>().Distinct().ToArray();
        var modes = selected is null ? [] :
            (await procedures.SpRec_TiposAlumnosModalidadesAsync(selected, cancellationToken: ct) ?? [])
                .Select(row => row.Modalidad?.Trim()).Where(IsConcrete).Cast<string>()
                .Distinct().ToArray();
        return new(types, modes, years);
    }
}
