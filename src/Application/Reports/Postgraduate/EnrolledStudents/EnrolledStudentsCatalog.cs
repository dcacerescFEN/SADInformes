using Application.Common.Interfaces;
using Application.Common.Validation;
using Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudentsFilters;

namespace Application.Reports.Postgraduate.EnrolledStudents;

internal static class EnrolledStudentsCatalog
{
    internal static readonly string[] Modalities = ["FOR THE AMERICAS", "SUNRISE"];

    internal static string RequiredModality(string? value)
    {
        var text = ConcreteFilter.Required("Seleccione una modalidad.", value);
        if (!Modalities.Contains(text, StringComparer.Ordinal))
            throw new ArgumentException("La modalidad no es válida.");
        return text;
    }

    internal static bool IsYear(string? value) =>
        value is { Length: 4 } && value.All(char.IsAsciiDigit) && value != "0000";

    internal static string RequiredYear(string? value)
    {
        var text = Required(value, "Seleccione un año.", 4);
        if (!IsYear(text)) throw new ArgumentException("El año debe tener cuatro dígitos.");
        return text;
    }

    internal static string Required(string? value, string message, int maxLength)
    {
        var text = ConcreteFilter.Required(message, value);
        if (text.Length > maxLength || text.Any(char.IsControl)) throw new ArgumentException(message);
        return text;
    }

    internal static async Task<EnrolledStudentsFiltersDTO> ReadAsync(
        ISadContextProcedures procedures, CancellationToken ct)
    {
        var years = (await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(IsYear).Cast<string>().Distinct().ToArray();
        var modalities = Modalities
            .Select(value => new EnrolledStudentsOptionDTO(value, value)).ToArray();
        return new(modalities, years);
    }
}
