using Application.Common.Interfaces;
using Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalDataFilters;

namespace Application.Reports.Postgraduate.PersonalData;

internal static class PersonalDataCatalog
{
    internal static async Task<PersonalDataFiltersDTO> ReadAsync(
        ISadContextProcedures procedures, string? studentType, CancellationToken ct)
    {
        var types = (await procedures.SpRec_TiposAlumnoXEscuelaAsync("POSTGRADO", "SI", cancellationToken: ct) ?? [])
            .Select(row => new PersonalDataOptionDTO(row.Tipo.Trim(), row.Nombre?.Trim() ?? row.Tipo.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        var selected = string.IsNullOrWhiteSpace(studentType) ? types.FirstOrDefault()?.Value : studentType.Trim();
        if (selected is not null && !types.Any(row => row.Value == selected))
            throw new ArgumentException("El tipo de alumno no es válido.");
        var modalities = selected is null or "Todos"
            ? ["Todos"]
            : (await procedures.spRec_EncuestaGraduados_ModalidadesAsync(selected, cancellationToken: ct) ?? [])
                .Select(row => row.Modalidad?.Trim()).Where(value => !string.IsNullOrEmpty(value) && value != "Todos")
                .Cast<string>().Distinct().Prepend("Todos").ToArray();
        var years = (await procedures.SpRec_AnosAsync("SI", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(value => value is not null
                && (value == "Todos" || (value.Length == 4 && value.All(char.IsAsciiDigit))))
            .Cast<string>().Distinct().ToArray();
        return new(types, modalities, years);
    }

    internal static string Value(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? "Todos" : value.Trim();
}
