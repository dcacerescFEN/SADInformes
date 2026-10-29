using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSample;

public sealed record GetExitSurveyStatisticsSample(string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<ExitSurveyStatisticsSampleDTO>>;
public sealed class GetExitSurveyStatisticsSampleHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsSample, ApiResponse<ExitSurveyStatisticsSampleDTO>>
{
    public async Task<ApiResponse<ExitSurveyStatisticsSampleDTO>> Handle(GetExitSurveyStatisticsSample request, CancellationToken ct)
    {
        var data = await ExitSurveyStatisticsRules.ReadSampleAsync(procedures, request.Period, request.StudentType, request.Modality, ct);
        var rows = data.Select(row => new ExitSurveySampleRowDTO(row.Type, row.Modality, row.Sample, row.InProgress, row.Answered)).ToArray();
        return responses.Success(new ExitSurveyStatisticsSampleDTO(rows,
            new ExitSurveySampleTotalsDTO(rows.Sum(row => row.Sample), rows.Sum(row => row.InProgress), rows.Sum(row => row.Answered))));
    }
}
