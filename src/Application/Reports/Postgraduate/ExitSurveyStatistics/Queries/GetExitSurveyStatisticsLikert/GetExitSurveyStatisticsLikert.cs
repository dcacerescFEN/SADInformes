using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsLikert;

public sealed record GetExitSurveyStatisticsLikert(string? Section, string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<ExitSurveyLikertDTO>>;
public sealed class GetExitSurveyStatisticsLikertHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsLikert, ApiResponse<ExitSurveyLikertDTO>>
{
    public async Task<ApiResponse<ExitSurveyLikertDTO>> Handle(GetExitSurveyStatisticsLikert request, CancellationToken ct)
    {
        var section = LikertSections.Required(request.Section);
        var period = ExitSurveyStatisticsRules.Text(request.Period, "El período no es válido.", 5);
        var type = ExitSurveyStatisticsRules.Type(ExitSurveyStatisticsRules.Text(request.StudentType, "El tipo de alumno no es válido.", 30));
        var modality = ExitSurveyStatisticsRules.Text(request.Modality, "La modalidad no es válida.", 10);
        var data = await LikertSections.ReadAsync(procedures, section, period, type, modality, ct);
        var rows = data.Select(row => new ExitSurveyLikertRowDTO(row.Question, row.Description,
            row.A1, row.A2, row.A3, row.A4, row.A5, row.Average, row.Total)).ToArray();
        return responses.Success(new ExitSurveyLikertDTO(section, LikertSections.Title(section), rows,
            new ExitSurveyLikertTotalsDTO(rows.Sum(row => row.Alternative1), rows.Sum(row => row.Alternative2),
                rows.Sum(row => row.Alternative3), rows.Sum(row => row.Alternative4), rows.Sum(row => row.Alternative5))));
    }
}
