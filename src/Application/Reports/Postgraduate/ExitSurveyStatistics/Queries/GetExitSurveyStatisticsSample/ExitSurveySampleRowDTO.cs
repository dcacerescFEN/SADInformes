namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsSample;
public sealed record ExitSurveySampleRowDTO(string Type, string? Modality, int Sample, int InProgress, int Answered);
