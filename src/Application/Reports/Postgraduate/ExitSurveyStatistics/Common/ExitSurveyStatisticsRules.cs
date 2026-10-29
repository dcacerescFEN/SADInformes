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
internal sealed record LikertRow(string Question, string Description, int A1, int A2, int A3, int A4, int A5, double? Average, int Total);

internal static class LikertSections
{
    internal static readonly string[] Ids = ["malla", "cuerpo-academico", "funcionamiento", "infraestructura", "servicios", "vinculacion-medio", "networking"];
    internal static string Title(string id) => id switch
    {
        "malla" => "Malla Curricular",
        "cuerpo-academico" => "Cuerpo Académico",
        "funcionamiento" => "Funcionamiento",
        "infraestructura" => "Infraestructura y recursos",
        "servicios" => "Servicios",
        "vinculacion-medio" => "Vinculación Medio",
        "networking" => "Networking",
        _ => throw new ArgumentException("La sección Likert no es válida."),
    };
    internal static string Required(string? id) => id is not null && Ids.Contains(id, StringComparer.Ordinal)
        ? id : throw new ArgumentException("La sección Likert no es válida.");

    internal static async Task<LikertRow[]> ReadAsync(ISadContextProcedures p, string section,
        string? period, string? type, string? modality, CancellationToken ct)
    {
        var rows = section switch
        {
            "malla" => await p.spRec_EncuestaGraduadosEstadisticasQ1Async(period, type, modality, cancellationToken: ct),
            "cuerpo-academico" => await p.spRec_EncuestaGraduadosEstadisticasQ2Async(period, type, modality, cancellationToken: ct),
            "funcionamiento" => await p.spRec_EncuestaGraduadosEstadisticasQ3Async(period, type, modality, cancellationToken: ct),
            "infraestructura" => await p.spRec_EncuestaGraduadosEstadisticasQ4Async(period, type, modality, cancellationToken: ct),
            "servicios" => await p.spRec_EncuestaGraduadosEstadisticasQ5Async(period, type, modality, cancellationToken: ct),
            "vinculacion-medio" => await p.spRec_EncuestaGraduadosEstadisticasQ6Async(period, type, modality, cancellationToken: ct),
            "networking" => await p.spRec_EncuestaGraduadosEstadisticasQ7Async(period, type, modality, cancellationToken: ct),
            _ => throw new ArgumentException("La sección Likert no es válida."),
        };
        return (rows ?? []).Select(row => new LikertRow(row.Pregunta?.Trim() ?? "", row.Descripcion?.Trim() ?? "",
            row.Alternativa1 ?? 0, row.Alternativa2 ?? 0, row.Alternativa3 ?? 0, row.Alternativa4 ?? 0,
            row.Alternativa5 ?? 0, row.Promedio, row.Total ?? 0)).ToArray();
    }
}
