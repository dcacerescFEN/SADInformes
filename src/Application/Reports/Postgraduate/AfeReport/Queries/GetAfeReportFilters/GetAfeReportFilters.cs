using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReportFilters;

public sealed record GetAfeReportFilters(string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<AfeReportFiltersDTO>>;

public sealed class GetAfeReportFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetAfeReportFilters, ApiResponse<AfeReportFiltersDTO>>
{
    public async Task<ApiResponse<AfeReportFiltersDTO>> Handle(
        GetAfeReportFilters request, CancellationToken ct)
    {
        var periodRows = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: ct) ?? [];
        var periods = periodRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Periodo) && row.Periodo.Trim() != "Todos")
            .Select(row => new AfeReportOptionDTO(
                row.Periodo.Trim(),
                string.IsNullOrWhiteSpace(row.PeriodoLargo) ? row.Periodo.Trim() : row.PeriodoLargo.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        var period = Select(periods, request.Period, "El período no es válido.");

        var typeRows = await procedures.SpRec_TiposAlumnoXEscuelaAsync(
            "POSTGRADO", "SI", cancellationToken: ct) ?? [];
        var types = typeRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Tipo))
            .Select(row => new AfeReportOptionDTO(
                row.Tipo.Trim(), string.IsNullOrWhiteSpace(row.Nombre) ? row.Tipo.Trim() : row.Nombre.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        var type = Select(types, request.StudentType, "El tipo de alumno no es válido.");

        var modalityRows = type is null or "Todos"
            ? []
            : await procedures.spRec_EncuestaGraduados_ModalidadesAsync(type, cancellationToken: ct) ?? [];
        var modalities = modalityRows
            .Select(row => row.Modalidad?.Trim())
            .Where(value => !string.IsNullOrEmpty(value) && value != "Todos")
            .Cast<string>().Distinct().Prepend("Todos")
            .Select(value => new AfeReportOptionDTO(value, value)).ToArray();
        var modality = Select(modalities, request.Modality, "La modalidad no es válida.");

        var courseRows = period is null || type is null || modality is null
            ? []
            : await procedures.SpRec_AfePorPeriodoTipoAlumnoAsync(
                period, type, modality, cancellationToken: ct) ?? [];
        var courses = courseRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Curso))
            .Select(row => new AfeReportOptionDTO(row.Curso.Trim(), row.Curso.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        return responses.Success(new AfeReportFiltersDTO(periods, types, modalities, courses));
    }

    private static string? Select(AfeReportOptionDTO[] options, string? requested, string error)
    {
        if (options.Length == 0) return null;
        var value = requested?.Trim();
        if (string.IsNullOrEmpty(value)) return options[0].Value;
        if (!options.Any(row => string.Equals(row.Value, value, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException(error);
        return options.First(row => string.Equals(row.Value, value, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
