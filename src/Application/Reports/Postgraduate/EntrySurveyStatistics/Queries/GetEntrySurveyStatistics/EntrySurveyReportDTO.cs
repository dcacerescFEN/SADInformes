namespace Application.Reports.Postgraduate.EntrySurveyStatistics.Queries.GetEntrySurveyStatistics;

public sealed record EntrySurveyReportDTO(EntrySurveyDTO[] Rows, int Total);
