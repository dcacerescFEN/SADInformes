using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeachingSchedule.Queries.GetTeachingSchedule;

public sealed record GetTeachingSchedule(string PeriodType, string Period, string Program, string? Modality, string? Promotion)
    : IRequest<ApiResponse<TeachingScheduleReportDTO>>;

public sealed class GetTeachingScheduleHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeachingSchedule, ApiResponse<TeachingScheduleReportDTO>>
{
    public async Task<ApiResponse<TeachingScheduleReportDTO>> Handle(GetTeachingSchedule request, CancellationToken ct)
    {
        TeachingScheduleValidation.PeriodType(request.PeriodType);
        TeachingScheduleValidation.Text(request.Period, 20, required: true);
        TeachingScheduleValidation.Text(request.Program, 30, required: true);
        TeachingScheduleValidation.Text(request.Modality, 30);
        // Legacy sends Descripcion (up to 50 chars), not the promotion code.
        // The canonical SQL wrapper deliberately retains varchar(7); do not truncate here.
        TeachingScheduleValidation.Text(request.Promotion, 50);
        var rows = await procedures.SpRec_ProgramacionDocenteXProgramaModalidadAsync(
            request.Period, request.PeriodType, request.Program, request.Modality, request.Promotion, cancellationToken: ct) ?? [];
        var result = rows.Select(row => new TeachingScheduleDTO(
            row.Periodo, row.PeriodoLargo ?? "", row.Curso, row.Nom_CatedraHija ?? "",
            row.Tipo ?? "", row.Ud, row.Profesores ?? "", row.Clases ?? "")).ToArray();
        return responses.Success(new TeachingScheduleReportDTO(result, result.Length));
    }
}
