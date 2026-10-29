namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSkills;
public sealed record ExitSurveySkillRowDTO(string Question, string Description,
    int Alternative1, int Alternative2, int Alternative3, int Alternative4, int Alternative5,
    int Alternative6, int Alternative7, double? Average, int Total);
