namespace Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluation;

public sealed record FullTimeCourseEvaluationDTO(
    string Period, string CourseCode, string Section, string ProfessorCode, string ProfessorName,
    string CourseName, string Modality, string StudentType, string Enrolled, string CourseAnswers,
    string CoursePercentage, string TeacherAnswers, string TeacherPercentage,
    string C1_1, string C1_2, string C1_3, string C1_4, string C1_5, string C1_6, string C1_7, string C1_8,
    string C1_9, string C1_10, string C1_11, string C1_12, string C1_13, string C1_14, string C1_15,
    string C1_16, string C1_17, string C1_18, string C1_19, string C1_20, string C1_21, string C1_22,
    string C1_23, string P1_1, string P1_2, string P1_3, string P1_4, string P1_5, string P1_6, string P1_7,
    string P1_8, string P1_9, string P1_10, string P1_11, string P1_12, string P1_13, string Grade);
