using Application.Common.Interfaces;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Common;

internal static class ExitSurveyStatisticsRules
{
    internal static readonly string[] StudentTypes =
    [
        "Todos", "MGP", "MCG", "MKT", "MF", "MTB", "MBA", "DOCTORADO",
        "ABOGADO", "MPP", "MGE", "PCI", "MAE",
    ];

    internal static string Text(string? value, string message, int maxLength)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Todos" : value.Trim();
        if (text.Length > maxLength || text.Any(char.IsControl)) throw new ArgumentException(message);
        return text;
    }

    internal static string Type(string? value) => StudentTypes.Contains(value?.Trim() ?? "", StringComparer.Ordinal)
        ? value!.Trim() : throw new ArgumentException("El tipo de alumno no es válido.");

    internal static async Task<Filters> ReadFiltersAsync(ISadContextProcedures procedures, string? requestedType, CancellationToken ct)
    {
        var type = string.IsNullOrWhiteSpace(requestedType) ? "Todos" : Type(requestedType);
        var periods = (await procedures.spRec_EncuestaGraduados_PeriodosAsync(cancellationToken: ct) ?? [])
            .Where(row => !string.IsNullOrWhiteSpace(row.Periodo))
            .Select(row => new Option(row.Periodo!.Trim(), row.Descripcion?.Trim() ?? row.Periodo.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        var modalities = (await procedures.spRec_EncuestaGraduados_ModalidadesAsync(type, cancellationToken: ct) ?? [])
            .Where(row => !string.IsNullOrWhiteSpace(row.Modalidad))
            .Select(row => new Option(row.Modalidad!.Trim(), row.Modalidad.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        return new(periods, StudentTypes.Select(value => new Option(value, value)).ToArray(), modalities, type);
    }

    internal static async Task<SampleRow[]> ReadSampleAsync(ISadContextProcedures procedures, string? period, string? type, string? modality, CancellationToken ct)
    {
        var rows = await procedures.spRec_EncuestaGraduados_EstadisticasAsync(
            Text(period, "El período no es válido.", 5),
            Type(Text(type, "El tipo de alumno no es válido.", 30)),
            Text(modality, "La modalidad no es válida.", 10), cancellationToken: ct) ?? [];
        return rows.Select(row => new SampleRow(row.Tipo?.Trim() ?? "", row.Modalidad?.Trim(),
            row.Muestra ?? 0, row.EnProceso ?? 0, row.Contestadas ?? 0)).ToArray();
    }
}

internal sealed record Option(string Value, string Text);
internal sealed record Filters(Option[] Periods, Option[] StudentTypes, Option[] Modalities, string SelectedType);
internal sealed record SampleRow(string Type, string? Modality, int Sample, int InProgress, int Answered);
