using Application.Common.Interfaces;
using Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPaymentFilters;

namespace Application.Reports.Postgraduate.BasicRightPayment;

internal static class BasicRightPaymentCatalog
{
    internal static async Task<BasicRightPaymentFiltersDTO> ReadAsync(
        ISadContextProcedures procedures, string? studentType, CancellationToken ct)
    {
        var typeRows = await procedures.SpRec_TiposAlumnoXEscuelaAsync(
            "POSTGRADO", "SI", cancellationToken: ct) ?? [];
        var types = typeRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Tipo) && row.Tipo.Trim() != "Todos")
            .Select(row => new BasicRightOptionDTO(
                row.Tipo.Trim(), string.IsNullOrWhiteSpace(row.Nombre) ? row.Tipo.Trim() : row.Nombre.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        var selected = string.IsNullOrWhiteSpace(studentType) ? types.FirstOrDefault()?.Value : studentType.Trim();
        if (selected is not null && !types.Any(row => row.Value == selected))
            throw new ArgumentException("El tipo de alumno no es válido.");
        var modalityRows = selected is null
            ? []
            : await procedures.SpRec_TiposAlumnosModalidadesAsync(selected, cancellationToken: ct) ?? [];
        var modalities = modalityRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Modalidad))
            .Select(row => row.Modalidad.Trim()).Distinct()
            .Select(value => new BasicRightOptionDTO(value, value)).ToArray();
        var yearRows = await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [];
        var years = yearRows.Select(row => row.Año?.Trim())
            .Where(value => value is { Length: 4 } && value.All(char.IsAsciiDigit))
            .Cast<string>().Distinct()
            .Select(value => new BasicRightOptionDTO(value, value)).ToArray();
        var statuses = new[]
        {
            new BasicRightOptionDTO("Todos", "Todos"),
            new BasicRightOptionDTO("Si", "Pagados"),
            new BasicRightOptionDTO("No", "Sin Pago"),
        };
        return new(types, modalities, years, statuses);
    }

    internal static string Value(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message) : value.Trim();
    internal static string Year(string? value, string message)
    {
        var year = Value(value, message);
        return year.Length == 4 && year.All(char.IsAsciiDigit) ? year : throw new ArgumentException(message);
    }
}
