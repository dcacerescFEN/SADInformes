using Application.Common.Interfaces;
using Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefensesFilters;

namespace Application.Reports.Postgraduate.ThesisDefenses;

internal static class ThesisDefensesCatalog
{
    internal static bool IsYear(string? value) =>
        value is { Length: 4 } && value.All(char.IsAsciiDigit) && value != "0000";

    internal static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Todos" : value.Trim();

    internal static async Task<ThesisDefensesFiltersDTO> ReadAsync(
        ISadContextProcedures procedures, string? studentType, CancellationToken ct)
    {
        var years = (await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(IsYear).Cast<string>().Distinct().ToArray();
        var types = (await procedures.SpRecTipoAlumnosPostgradoAsync(cancellationToken: ct) ?? [])
            .Select(row => row.Tipo_Alumno.Trim()).Where(value => value.Length > 0)
            .Distinct().Prepend("Todos").ToArray();
        var selected = Normalize(studentType);
        if (!types.Contains(selected)) throw new ArgumentException("El tipo de alumno no es válido.");
        var modalities = selected == "Todos"
            ? ["Todos"]
            : (await procedures.SpRecModalidadesPostgradoAsync(selected, cancellationToken: ct) ?? [])
                .Select(row => row.Modalidad?.Trim()).Where(value => !string.IsNullOrEmpty(value))
                .Cast<string>().Distinct().Prepend("Todos").ToArray();
        return new(years, types, modalities);
    }

    internal static async Task<(string DefenseYear, string StudentType, string Modality, string EntryYear)> ValidateAsync(
        ISadContextProcedures procedures, string? defenseYear, string? studentType,
        string? modality, string? entryYear, CancellationToken ct)
    {
        var defense = Normalize(defenseYear);
        var entry = Normalize(entryYear);
        var type = Normalize(studentType);
        var mode = Normalize(modality);
        if (defense != "Todos" && !IsYear(defense))
            throw new ArgumentException("El año de defensa no es válido.");
        if (entry != "Todos" && !IsYear(entry))
            throw new ArgumentException("El año de ingreso no es válido.");
        var filters = await ReadAsync(procedures, type, ct);
        if (defense != "Todos" && !filters.Years.Contains(defense))
            throw new ArgumentException("Seleccione un año de defensa válido.");
        if (entry != "Todos" && !filters.Years.Contains(entry))
            throw new ArgumentException("Seleccione un año de ingreso válido.");
        if (!filters.Modalities.Contains(mode))
            throw new ArgumentException("Seleccione una modalidad válida.");
        return (defense, type, mode, entry);
    }
}
