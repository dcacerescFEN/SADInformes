namespace Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsSummary;

public sealed record IssStatisticsCategoryDTO(
    int Id, string Description, IssStatisticsQuestionDTO[] Questions);
