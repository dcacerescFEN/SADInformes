using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRanking;

public sealed record GetWeightedRanking(string? StudentType, string? Modality, string? Cohort, string? Year)
    : IRequest<ApiResponse<WeightedRankingReportDTO>>;

public sealed class GetWeightedRankingHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetWeightedRanking, ApiResponse<WeightedRankingReportDTO>>
{
    public async Task<ApiResponse<WeightedRankingReportDTO>> Handle(
        GetWeightedRanking request, CancellationToken ct)
    {
        var type = WeightedRankingCatalog.Required(
            request.StudentType, "Seleccione un tipo de alumno.", 30);
        var modality = WeightedRankingCatalog.Required(
            request.Modality, "Seleccione una modalidad.", 30);
        var cohort = WeightedRankingCatalog.Required(
            request.Cohort, "Seleccione un año de ingreso.", 4);
        var year = WeightedRankingCatalog.Required(
            request.Year, "Seleccione un año del cuadro de honor.", 4);
        if (!WeightedRankingCatalog.IsYear(cohort) || !WeightedRankingCatalog.IsYear(year))
            throw new ArgumentException("Los años deben tener cuatro dígitos.");

        var filters = await WeightedRankingCatalog.ReadAsync(procedures, type, ct);
        if (!filters.Modalities.Contains(modality)
            || !filters.Years.Contains(cohort)
            || !filters.Years.Contains(year))
            throw new ArgumentException("Seleccione filtros válidos del catálogo.");

        var data = await procedures.SpRec_AlumnosCuadroDeHonorPosgradoAsync(
            type, modality, cohort, year, cancellationToken: ct) ?? [];
        // Preserve procedure order, positions and decimal scales; never re-rank or recompute percentile.
        var rows = data.Select(row => new WeightedRankingDTO(
            row.Posicion,
            row.Nombre?.Trim() ?? "",
            row.Rut?.Trim() ?? "",
            row.Codigo?.Trim() ?? "",
            row.Ingreso?.Trim() ?? "",
            row.Situacion?.Trim() ?? "",
            Format(row.Promedio, "0.000"),
            Format(row.Percentil, "0.0000"))).ToArray();
        return responses.Success(new WeightedRankingReportDTO(rows, rows.Length));
    }

    private static string Format(decimal? value, string format) =>
        value?.ToString(format, CultureInfo.InvariantCulture) ?? "";
}
