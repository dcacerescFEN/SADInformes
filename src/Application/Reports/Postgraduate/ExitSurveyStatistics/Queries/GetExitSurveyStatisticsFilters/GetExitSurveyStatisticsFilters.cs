using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsFilters;

public sealed record GetExitSurveyStatisticsFilters(string? StudentType) : IRequest<ApiResponse<ExitSurveyStatisticsFiltersDTO>>;
public sealed class GetExitSurveyStatisticsFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsFilters, ApiResponse<ExitSurveyStatisticsFiltersDTO>>
{
    public async Task<ApiResponse<ExitSurveyStatisticsFiltersDTO>> Handle(GetExitSurveyStatisticsFilters request, CancellationToken ct)
    {
        var filters = await ExitSurveyStatisticsRules.ReadFiltersAsync(procedures, request.StudentType, ct);
        return responses.Success(new ExitSurveyStatisticsFiltersDTO(
            filters.Periods.Select(row => new ExitSurveyStatisticsOptionDTO(row.Value, row.Text)).ToArray(),
            filters.StudentTypes.Select(row => new ExitSurveyStatisticsOptionDTO(row.Value, row.Text)).ToArray(),
            filters.Modalities.Select(row => new ExitSurveyStatisticsOptionDTO(row.Value, row.Text)).ToArray()));
    }
}
