using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeachingSchedule.Queries.GetTeachingScheduleFilters;

public sealed record GetTeachingScheduleFilters(string PeriodType, string? Program)
    : IRequest<ApiResponse<TeachingScheduleFiltersDTO>>;

public sealed class GetTeachingScheduleFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeachingScheduleFilters, ApiResponse<TeachingScheduleFiltersDTO>>
{
    public async Task<ApiResponse<TeachingScheduleFiltersDTO>> Handle(GetTeachingScheduleFilters request, CancellationToken ct)
    {
        TeachingScheduleValidation.PeriodType(request.PeriodType);
        TeachingScheduleValidation.Text(request.Program, 30);
        var periods = await procedures.SpRec_PeriodosAcademicosXTipoAsync(request.PeriodType, "POSTGRADO", "19000", cancellationToken: ct) ?? [];
        var programs = await procedures.SpRec_TiposAlumnoXEscuelaAsync("POSTGRADO", "NO", cancellationToken: ct) ?? [];
        var promotions = await procedures.SpRec_PromocionesPostgradoTodosAsync(cancellationToken: ct) ?? [];
        var modalities = string.IsNullOrEmpty(request.Program) ? []
            : await procedures.SpRec_TiposAlumnosModalidadesAsync(request.Program, cancellationToken: ct) ?? [];
        return responses.Success(new TeachingScheduleFiltersDTO(
            [new("PERÍODO", "Bimestre"), new("SEMESTRE", "Semestre"), new("AÑO", "Año")],
            periods.Select(row => new TeachingScheduleOptionDTO(row.Periodo ?? "", row.PeriodoLargo ?? row.Periodo ?? "")).ToArray(),
            programs.Select(row => new TeachingScheduleOptionDTO(row.Tipo, row.Nombre ?? row.Tipo)).ToArray(),
            modalities.Select(row => row.Modalidad ?? "").ToArray(),
            promotions.Select(row => new TeachingScheduleOptionDTO(row.Descripcion, row.Promocion)).ToArray()));
    }
}
