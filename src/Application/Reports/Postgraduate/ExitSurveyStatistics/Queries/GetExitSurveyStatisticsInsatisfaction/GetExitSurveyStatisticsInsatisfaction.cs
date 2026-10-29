using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsInsatisfaction;

public sealed record GetExitSurveyStatisticsInsatisfaction(string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<ExitSurveyInsatisfactionDTO>>;
public sealed class GetExitSurveyStatisticsInsatisfactionHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsInsatisfaction, ApiResponse<ExitSurveyInsatisfactionDTO>>
{
    public async Task<ApiResponse<ExitSurveyInsatisfactionDTO>> Handle(GetExitSurveyStatisticsInsatisfaction request, CancellationToken ct)
    {
        var data = await procedures.spRec_EncuestaGraduadosEstadisticasInsatisfaccionAsync(
            ExitSurveyStatisticsRules.Text(request.Period, "El período no es válido.", 5),
            ExitSurveyStatisticsRules.Type(ExitSurveyStatisticsRules.Text(request.StudentType, "El tipo de alumno no es válido.", 30)),
            ExitSurveyStatisticsRules.Text(request.Modality, "La modalidad no es válida.", 10), cancellationToken: ct) ?? [];
        var rows = data.Select(row => new ExitSurveyInsatisfactionRowDTO(row.Profesor?.Trim() ?? "", row.Cantidad ?? 0,
            row.CodPersona?.Trim() ?? "", row.Periodo?.Trim() ?? "", row.TipoAlumno?.Trim() ?? "", row.Modalidad?.Trim() ?? "")).ToArray();
        return responses.Success(new ExitSurveyInsatisfactionDTO(rows));
    }
}
