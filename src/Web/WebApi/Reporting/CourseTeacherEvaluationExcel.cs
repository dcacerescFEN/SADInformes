using Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluation;

namespace WebApi.Reporting;

public static class CourseTeacherEvaluationExcel
{
    private static readonly string[] BaseHeaders = ["Periodo", "Cod_Catedra", "Cod_Seccion", "Cod_Profesor",
        "Nombre", "Nombre Catedra", "Tipo Alumno", "Modalidad", "Inscritos", "Contestadas Curso",
        "PorcentajeCurso", "ContestadasDocente", "PorcentajeDocente"];

    public static byte[] Executive(IReadOnlyList<ExecutiveCourseEvaluationDTO> rows)
    {
        var headers = BaseHeaders.Concat(["C1_1", "C1_2", "C1_3", "C1_4",
            "P1_1", "P1_2", "P1_3", "P1_4", "P1_5", "P1_6", "P1_7", "P1_8", "P1_9", "P1_10",
            "P1_11", "P1_12", "P1_13", "Nota"]).ToArray();
        return TextWorkbook.Create("Evaluacion_Docente_Curso", headers,
            rows.Select(row => new string?[]
            {
                row.Period, row.CourseCode, row.Section, row.ProfessorCode, row.ProfessorName,
                row.CourseName, row.StudentType, row.Modality, row.Enrolled, row.CourseAnswers,
                row.CoursePercentage, row.TeacherAnswers, row.TeacherPercentage, row.C1_1, row.C1_2,
                row.C1_3, row.C1_4, row.P1_1, row.P1_2, row.P1_3, row.P1_4, row.P1_5, row.P1_6,
                row.P1_7, row.P1_8, row.P1_9, row.P1_10, row.P1_11, row.P1_12, row.P1_13, row.Grade,
            }));
    }

    public static byte[] FullTime(IReadOnlyList<FullTimeCourseEvaluationDTO> rows)
    {
        var headers = BaseHeaders.Concat(Enumerable.Range(1, 23).Select(index => "C1_" + index))
            .Concat(Enumerable.Range(1, 13).Select(index => "P1_" + index)).Append("Nota").ToArray();
        return TextWorkbook.Create("Evaluacion_Docente_Curso", headers,
            rows.Select(row => new string?[]
            {
                row.Period, row.CourseCode, row.Section, row.ProfessorCode, row.ProfessorName,
                row.CourseName, row.StudentType, row.Modality, row.Enrolled, row.CourseAnswers,
                row.CoursePercentage, row.TeacherAnswers, row.TeacherPercentage,
                row.C1_1, row.C1_2, row.C1_3, row.C1_4, row.C1_5, row.C1_6, row.C1_7, row.C1_8,
                row.C1_9, row.C1_10, row.C1_11, row.C1_12, row.C1_13, row.C1_14, row.C1_15,
                row.C1_16, row.C1_17, row.C1_18, row.C1_19, row.C1_20, row.C1_21, row.C1_22,
                row.C1_23, row.P1_1, row.P1_2, row.P1_3, row.P1_4, row.P1_5, row.P1_6, row.P1_7,
                row.P1_8, row.P1_9, row.P1_10, row.P1_11, row.P1_12, row.P1_13, row.Grade,
            }));
    }
}
