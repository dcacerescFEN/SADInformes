using Application.Common.Interfaces;
using Application.Common.Validation;
using Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValiditiesFilters;

namespace Application.Reports.Postgraduate.TeacherValidities;

internal static class TeacherValiditiesCatalog
{
    internal static bool IsYear(string? value) =>
        value is { Length: 4 } && value.All(char.IsAsciiDigit) && value != "0000";

    internal static string RequiredYear(string? value)
    {
        var text = ConcreteFilter.Required("Seleccione un año.", value);
        if (text.Length > 4 || text.Any(char.IsControl) || !IsYear(text))
            throw new ArgumentException("El año debe tener cuatro dígitos.");
        return text;
    }

    internal static async Task<TeacherValiditiesFiltersDTO> ReadAsync(
        ISadContextProcedures procedures, CancellationToken ct)
    {
        var years = (await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(IsYear).Cast<string>().Distinct().ToArray();
        return new(years);
    }
}
