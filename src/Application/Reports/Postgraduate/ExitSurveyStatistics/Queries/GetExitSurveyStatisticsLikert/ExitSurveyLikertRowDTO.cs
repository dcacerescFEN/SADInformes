namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsLikert;
public sealed record ExitSurveyLikertRowDTO(string Question, string Description,
    int Alternative1, int Alternative2, int Alternative3, int Alternative4, int Alternative5,
    double? Average, int Total);
