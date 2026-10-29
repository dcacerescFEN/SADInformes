namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsTeachers;
public sealed record ExitSurveyTeachersDTO(ExitSurveyTeacherDTO[] Theoretical,
    ExitSurveyTeacherDTO[] Practical, ExitSurveyTeacherDTO[] Formative);
