namespace Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsSummary;

public sealed record IssStatisticsQuestionDTO(
    string Question, int Alternative1, int Alternative2, int Alternative3, int Alternative4,
    int Alternative5, int Total, double? Average);
