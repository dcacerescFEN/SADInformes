using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsSummary;

public sealed record GetIssStatisticsSummary(string? Period, string? CourseSection)
    : IRequest<ApiResponse<IssStatisticsSummaryDTO>>;

public sealed class GetIssStatisticsSummaryHandler(
    ISadContextProcedures procedures, ISadContext context, IApiResponseService responses)
    : IRequestHandler<GetIssStatisticsSummary, ApiResponse<IssStatisticsSummaryDTO>>
{
    public async Task<ApiResponse<IssStatisticsSummaryDTO>> Handle(
        GetIssStatisticsSummary request, CancellationToken ct)
    {
        var period = request.Period?.Trim();
        if (string.IsNullOrEmpty(period))
            throw new ArgumentException("Seleccione un período.");
        var periodRows = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: ct) ?? [];
        if (!periodRows.Any(row => string.Equals(row.Periodo?.Trim(), period, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("El período no es válido.");

        var courseSection = request.CourseSection?.Trim();
        if (string.IsNullOrEmpty(courseSection))
            throw new ArgumentException("Seleccione un curso y sección.");
        var parts = courseSection.Split('/');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new ArgumentException("El curso y sección no es válido.");
        var sections = await procedures.SpRec_EncuestaISSCatedraSeccionAsync(period, cancellationToken: ct) ?? [];
        if (!sections.Any(row => string.Equals(row.CatedraSeccion?.Trim(), courseSection, StringComparison.Ordinal)))
            throw new ArgumentException("El curso y sección no es válido.");

        var sampleRow = (await procedures.SpRec_EncuestaISSEstadisticasMuestraAsync(
            period, courseSection, cancellationToken: ct))?.FirstOrDefault();
        var categoryRows = await procedures.SpRec_EncuestaISSCategoriasAsync(
            period, courseSection, cancellationToken: ct) ?? [];
        var categoryIds = categoryRows.Select(row => row.Id_Categoria).Distinct().ToArray();
        var descriptions = categoryIds.Length == 0
            ? new Dictionary<int, string>()
            : await context.EvaluacionIssPostgradoCategorias
                .Where(row => categoryIds.Contains(row.IdCategoria))
                .Select(row => new { row.IdCategoria, row.Descripcion })
                .ToDictionaryAsync(row => row.IdCategoria, row => row.Descripcion, ct);

        var categories = new List<IssStatisticsCategoryDTO>(categoryIds.Length);
        foreach (var categoryId in categoryIds)
        {
            var rows = await procedures.SpRec_EvaluacionISSEstadisticasAsync(
                period, parts[0].Trim(), parts[1].Trim(), categoryId, cancellationToken: ct) ?? [];
            categories.Add(new IssStatisticsCategoryDTO(
                categoryId,
                descriptions.TryGetValue(categoryId, out var description) && !string.IsNullOrWhiteSpace(description)
                    ? description.Trim() : $"Categoría {categoryId}",
                rows.Select(row => new IssStatisticsQuestionDTO(
                    row.Pregunta_descripcion?.Trim() ?? "", row.Alternativa1, row.Alternativa2,
                    row.Alternativa3, row.Alternativa4, row.Alternativa5, row.Total,
                    row.Promedio)).ToArray()));
        }

        var observations = (await procedures.SpRec_EvaluacionISSObservacionesAsync(
            period, parts[0].Trim(), parts[1].Trim(), cancellationToken: ct) ?? [])
            .Select(row => row.Observacion?.Trim() ?? "").ToArray();
        var sample = sampleRow is null ? null : new IssStatisticsSampleDTO(
            sampleRow.Cod_Catedra.Trim(), sampleRow.Cod_Seccion.Trim(), sampleRow.Periodo.Trim(),
            sampleRow.Inscritos, sampleRow.Contestadas, sampleRow.Porcentaje, sampleRow.Faltantes);
        return responses.Success(new IssStatisticsSummaryDTO(sample, categories.ToArray(), observations));
    }
}
