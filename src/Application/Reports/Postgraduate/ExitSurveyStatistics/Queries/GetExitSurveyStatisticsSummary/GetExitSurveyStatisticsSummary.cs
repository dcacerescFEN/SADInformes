using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSummary;

public sealed record GetExitSurveyStatisticsSummary(string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<ExitSurveySummaryDTO>>;
public sealed class GetExitSurveyStatisticsSummaryHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsSummary, ApiResponse<ExitSurveySummaryDTO>>
{
    public async Task<ApiResponse<ExitSurveySummaryDTO>> Handle(GetExitSurveyStatisticsSummary request, CancellationToken ct)
    {
        var period = ExitSurveyStatisticsRules.Text(request.Period, "El período no es válido.", 5);
        var type = ExitSurveyStatisticsRules.Type(ExitSurveyStatisticsRules.Text(request.StudentType, "El tipo de alumno no es válido.", 30));
        var modality = ExitSurveyStatisticsRules.Text(request.Modality, "La modalidad no es válida.", 10);
        var rows = new List<ExitSurveySummaryRowDTO>();
        foreach (var section in LikertSections.Ids)
        {
            var data = await LikertSections.ReadAsync(procedures, section, period, type, modality, ct);
            var a1 = data.Sum(row => row.A1);
            var a2 = data.Sum(row => row.A2);
            var a3 = data.Sum(row => row.A3);
            var a4 = data.Sum(row => row.A4);
            var a5 = data.Sum(row => row.A5);
            var total = a1 + a2 + a3 + a4 + a5;
            rows.Add(new(section, LikertSections.Title(section), a1, a2, a3, a4, a5, total,
                total == 0 ? null : (a1 + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5) / (double)total));
        }
        return responses.Success(new ExitSurveySummaryDTO(rows.ToArray()));
    }
}
