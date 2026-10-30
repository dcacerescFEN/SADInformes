using Application.Reports.Postgraduate.ThesisRegistration.Queries.GetThesisRegistration;

namespace WebApi.Reporting;

public static class ThesisRegistrationExcel
{
    private static readonly string[] Headers =
    [
        "Cod_Alumno", "Matricula", "Sem_IngresoDecreto", "Decreto", "Cod_Persona", "Tipo_Alumno",
        "Modalidad", "Apellidos", "Nombres", "CatedraAFE", "PeriodoAFE", "ProfesorAFE", "NotaAFE",
        "Cod_Catedra", "Nota", "Periodo_Ins_Tesis", "Cod_SitAcademica", "Motivo", "Nombre_Tesis",
        "Profesores", "Fecha_Examen",
    ];

    public static byte[] Create(IReadOnlyList<ThesisRegistrationDTO> rows) =>
        TextWorkbook.Create("Registro Tesis", Headers, rows.Select(row => new string?[]
        {
            row.StudentCode, row.Enrollment, row.EntrySemester, row.Decree, row.PersonCode,
            row.StudentType, row.Modality, row.Surnames, row.Names, row.AfeCourse, row.AfePeriod,
            row.AfeTeacher, row.AfeGrade, row.CourseCode, row.ThesisEnrollmentPeriod,
            row.AcademicSituation, row.Reason, row.ThesisName, row.Grade, row.Teachers, row.ExamDate,
        }));
}
