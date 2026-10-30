namespace Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluation;

public sealed record CourseTeacherEvaluationReportDTO(
    string Type,
    ExecutiveCourseEvaluationDTO[] Executive,
    FullTimeCourseEvaluationDTO[] FullTime,
    int Total);
