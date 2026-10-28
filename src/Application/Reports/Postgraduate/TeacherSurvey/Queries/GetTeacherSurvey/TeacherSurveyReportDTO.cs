namespace Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurvey;

public sealed record TeacherSurveyReportDTO(IReadOnlyList<TeacherSurveyDTO> Rows, int Total);
