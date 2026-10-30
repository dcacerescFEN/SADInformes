using Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalData;

namespace WebApi.Reporting;

public static class PersonalDataExcel
{
    private static readonly string[] Headers =
    [
        "ID_Alumno", "Cod Alumno", "Semestre_Ingreso", "Cod_Persona", "Tipo_Alumno", "Modalidad",
        "Situacion_Academica", "Motivo", "Nombres", "Apellidos", "Email_Personal", "Email_Fen",
        "Email_laboral", "Celular", "Tel_laboral", "NomEmpresa", "CargoEmpresa", "Fecha_Nacimiento",
        "Nacionalidad", "Sexo", "Carrera Anterior", "Institución", "Fecha Resolucion", "Fecha Examen",
        "Ultimo Periodo Inscripcion Curso", "Periodo Examen",
    ];

    public static byte[] Create(IReadOnlyList<PersonalDataDTO> rows) =>
        TextWorkbook.Create("Datos Personales", Headers, rows.Select(row => new string?[]
        {
            row.IdAlumno, row.StudentCode, row.EntrySemester, row.PersonCode, row.StudentType,
            row.Modality, row.AcademicSituation, row.Reason, row.Names, row.Surnames,
            row.PersonalEmail, row.FenEmail, row.WorkEmail, row.Mobile, row.WorkPhone,
            row.Company, row.Position, row.BirthDate, row.Nationality, row.Sex, row.PreviousDegree,
            row.OriginUniversity, row.ResolutionDate, row.ExamDate, row.LastCoursePeriod, row.ExamPeriod,
        }));
}
