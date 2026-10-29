using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsCatedrasArea;

public sealed record GetExitSurveyStatisticsCatedrasArea(string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<ExitSurveyCatedrasAreaDTO>>;
public sealed class GetExitSurveyStatisticsCatedrasAreaHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsCatedrasArea, ApiResponse<ExitSurveyCatedrasAreaDTO>>
{
    public async Task<ApiResponse<ExitSurveyCatedrasAreaDTO>> Handle(GetExitSurveyStatisticsCatedrasArea request, CancellationToken ct)
    {
        var data = await procedures.spRec_EncuestaGraduadosEstadisticasQ8Async(
            ExitSurveyStatisticsRules.Text(request.Period, "El período no es válido.", 5),
            ExitSurveyStatisticsRules.Type(ExitSurveyStatisticsRules.Text(request.StudentType, "El tipo de alumno no es válido.", 30)),
            ExitSurveyStatisticsRules.Text(request.Modality, "La modalidad no es válida.", 10), cancellationToken: ct) ?? [];
        var rows = data.Select(row => new ExitSurveyCatedraRowDTO(row.CodCatedra?.Trim() ?? "", row.Descripcion?.Trim() ?? "",
            row.Alternativa1 ?? 0, row.Alternativa2 ?? 0, row.Alternativa3 ?? 0, row.Alternativa4 ?? 0,
            row.Alternativa5 ?? 0, row.Promedio, row.Total ?? 0)).ToArray();
        return responses.Success(new ExitSurveyCatedrasAreaDTO(rows,
            new ExitSurveyCatedraTotalsDTO(rows.Sum(row => row.Alternative1), rows.Sum(row => row.Alternative2),
                rows.Sum(row => row.Alternative3), rows.Sum(row => row.Alternative4), rows.Sum(row => row.Alternative5))));
    }
}
