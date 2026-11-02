namespace Application.Reports.Postgraduate.AlumniSurveyStatistics.Queries.GetAlumniSurvey;

public sealed record AlumniSurveyReportDTO(AlumniSurveyDTO[] Rows, int Total);
