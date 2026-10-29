namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsLikert;
public sealed record ExitSurveyLikertDTO(string Section, string Title, ExitSurveyLikertRowDTO[] Rows, ExitSurveyLikertTotalsDTO Totals);
