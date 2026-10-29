using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSkills;

public sealed record GetExitSurveyStatisticsSkills(string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<ExitSurveySkillsDTO>>;
public sealed class GetExitSurveyStatisticsSkillsHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsSkills, ApiResponse<ExitSurveySkillsDTO>>
{
    public async Task<ApiResponse<ExitSurveySkillsDTO>> Handle(GetExitSurveyStatisticsSkills request, CancellationToken ct)
    {
        var data = await procedures.spRec_EncuestaGraduadosEstadisticasQ11Async(
            ExitSurveyStatisticsRules.Text(request.Period, "El período no es válido.", 5),
            ExitSurveyStatisticsRules.Type(ExitSurveyStatisticsRules.Text(request.StudentType, "El tipo de alumno no es válido.", 30)),
            ExitSurveyStatisticsRules.Text(request.Modality, "La modalidad no es válida.", 10), cancellationToken: ct) ?? [];
        var rows = data.Select(row => new ExitSurveySkillRowDTO(row.Pregunta?.Trim() ?? "", row.Descripcion?.Trim() ?? "",
            row.Alternativa1 ?? 0, row.Alternativa2 ?? 0, row.Alternativa3 ?? 0, row.Alternativa4 ?? 0,
            row.Alternativa5 ?? 0, row.Alternativa6 ?? 0, row.Alternativa7 ?? 0, row.Promedio, row.Total ?? 0)).ToArray();
        return responses.Success(new ExitSurveySkillsDTO(rows,
            new ExitSurveySkillTotalsDTO(rows.Sum(row => row.Alternative1), rows.Sum(row => row.Alternative2),
                rows.Sum(row => row.Alternative3), rows.Sum(row => row.Alternative4), rows.Sum(row => row.Alternative5),
                rows.Sum(row => row.Alternative6), rows.Sum(row => row.Alternative7))));
    }
}
