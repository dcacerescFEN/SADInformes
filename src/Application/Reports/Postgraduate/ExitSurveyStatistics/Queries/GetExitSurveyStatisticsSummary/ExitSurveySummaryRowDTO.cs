namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSummary;
public sealed record ExitSurveySummaryRowDTO(string Section, string Title,
    int Alternative1, int Alternative2, int Alternative3, int Alternative4, int Alternative5,
    int Total, double? Dimension);
