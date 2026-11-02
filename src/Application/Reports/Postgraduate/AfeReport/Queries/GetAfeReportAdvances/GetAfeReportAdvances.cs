using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReportAdvances;

public sealed record GetAfeReportAdvances(string? Period, string? Course)
    : IRequest<ApiResponse<AfeReportAdvancesDTO>>;

public sealed class GetAfeReportAdvancesHandler(
    ISadContextProcedures procedures, IEnumerable<IDocenciaWebContext> docencia,
    IApiResponseService responses)
    : IRequestHandler<GetAfeReportAdvances, ApiResponse<AfeReportAdvancesDTO>>
{
    public async Task<ApiResponse<AfeReportAdvancesDTO>> Handle(
        GetAfeReportAdvances request, CancellationToken ct)
    {
        var db = docencia.LastOrDefault()
            ?? throw new InvalidOperationException("La conexión DocenciaWeb no está configurada.");
        var period = request.Period?.Trim();
        if (string.IsNullOrEmpty(period))
            throw new ArgumentException("Seleccione un período.");
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: ct) ?? [];
        if (!periods.Any(row => string.Equals(row.Periodo?.Trim(), period, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("El período no es válido.");
        var parts = ParseCourse(request.Course);
        var taskType = await db.TaskTipos.AsNoTracking()
            .Where(row => row.TipoTask == "AFE").Select(row => row.Id).FirstOrDefaultAsync(ct);
        if (taskType == 0) return responses.Success(new AfeReportAdvancesDTO([]));
        var ids = await db.TaskMs.AsNoTracking()
            .Where(row => row.Periodo == period && row.CodCatedra == parts.Course
                && row.CodSeccion == parts.Section && row.TipoTaskId == taskType
                && row.FechaElimina == null)
            .OrderBy(row => row.Id).Select(row => row.Id).ToListAsync(ct);
        var advances = ids.Select((id, index) =>
            new AfeAdvanceOptionDTO(id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))).ToList();
        if (advances.Count > 1) advances.Insert(0, new("-1", "Todos"));
        return responses.Success(new AfeReportAdvancesDTO(advances.ToArray()));
    }

    private static (string Course, string Section) ParseCourse(string? value)
    {
        var parts = value?.Trim().Split('/');
        if (parts is not { Length: 2 } || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new ArgumentException("Seleccione un curso y sección válidos.");
        return (parts[0].Trim(), parts[1].Trim());
    }
}
