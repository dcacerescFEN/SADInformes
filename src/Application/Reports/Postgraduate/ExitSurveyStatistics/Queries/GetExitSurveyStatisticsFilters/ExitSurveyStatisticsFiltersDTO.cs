namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsFilters;
public sealed record ExitSurveyStatisticsFiltersDTO(ExitSurveyStatisticsOptionDTO[] Periods,
    ExitSurveyStatisticsOptionDTO[] StudentTypes, ExitSurveyStatisticsOptionDTO[] Modalities);
