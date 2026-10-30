namespace Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluation;

public sealed record ExecutiveCourseEvaluationDTO(
    string Period, string CourseCode, string Section, string ProfessorCode, string ProfessorName,
    string CourseName, string Modality, string StudentType, string Enrolled, string CourseAnswers,
    string CoursePercentage, string TeacherAnswers, string TeacherPercentage, string C1_1, string C1_2,
    string C1_3, string C1_4, string P1_1, string P1_2, string P1_3, string P1_4, string P1_5, string P1_6,
    string P1_7, string P1_8, string P1_9, string P1_10, string P1_11, string P1_12, string P1_13, string Grade);
