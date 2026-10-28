namespace Application.Reports.Postgraduate.UnansweredTeacherSurvey.Queries.GetUnansweredTeacherSurvey;

public sealed record UnansweredTeacherSurveyReportDTO(IReadOnlyList<UnansweredTeacherSurveyDTO> Rows, int Total);
