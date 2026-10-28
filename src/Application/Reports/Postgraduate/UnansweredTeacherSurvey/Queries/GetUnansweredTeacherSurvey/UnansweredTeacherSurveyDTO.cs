namespace Application.Reports.Postgraduate.UnansweredTeacherSurvey.Queries.GetUnansweredTeacherSurvey;

public sealed record UnansweredTeacherSurveyDTO(
    string Period, string Course, string Name, string StudentCode, string StudentName);
