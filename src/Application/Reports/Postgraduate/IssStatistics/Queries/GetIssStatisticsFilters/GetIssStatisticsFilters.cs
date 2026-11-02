using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsFilters;

public sealed record GetIssStatisticsFilters(string? Period)
    : IRequest<ApiResponse<IssStatisticsFiltersDTO>>;

public sealed class GetIssStatisticsFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetIssStatisticsFilters, ApiResponse<IssStatisticsFiltersDTO>>
{
    public async Task<ApiResponse<IssStatisticsFiltersDTO>> Handle(
        GetIssStatisticsFilters request, CancellationToken ct)
    {
        var periodRows = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: ct) ?? [];
        var periods = periodRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Periodo) && row.Periodo.Trim() != "Todos")
            .Select(row => new IssStatisticsOptionDTO(
                row.Periodo.Trim(), string.IsNullOrWhiteSpace(row.PeriodoLargo) ? row.Periodo.Trim() : row.PeriodoLargo.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        var selected = request.Period?.Trim();
        if (string.IsNullOrEmpty(selected)) selected = periods.FirstOrDefault()?.Value;
        else if (!periods.Any(row => string.Equals(row.Value, selected, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("El período no es válido.");

        if (string.IsNullOrEmpty(selected)) return responses.Success(new IssStatisticsFiltersDTO(periods, []));

        var sectionRows = await procedures.SpRec_EncuestaISSCatedraSeccionAsync(selected, cancellationToken: ct) ?? [];
        var sections = sectionRows
            .Where(row => !string.IsNullOrWhiteSpace(row.CatedraSeccion))
            .Select(row => new IssStatisticsOptionDTO(
                row.CatedraSeccion.Trim(),
                string.IsNullOrWhiteSpace(row.Descripcion) ? row.CatedraSeccion.Trim() : row.Descripcion.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        return responses.Success(new IssStatisticsFiltersDTO(periods, sections));
    }
}
