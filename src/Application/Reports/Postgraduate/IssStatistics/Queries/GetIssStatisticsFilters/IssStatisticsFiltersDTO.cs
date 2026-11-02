namespace Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsFilters;

public sealed record IssStatisticsFiltersDTO(
    IssStatisticsOptionDTO[] Periods, IssStatisticsOptionDTO[] CourseSections);
