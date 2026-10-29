namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsInsatisfaction;
public sealed record ExitSurveyInsatisfactionRowDTO(string Professor, int Count, string ProfessorCode,
    string Period, string StudentType, string Modality);
