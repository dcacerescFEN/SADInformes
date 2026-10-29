using Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudents;

namespace WebApi.Reporting;

public static class EnrolledStudentsExcel
{
    private static readonly string[] Headers =
    [
        "Cod Alumno", "Semestre_Ingreso", "Cod_Persona", "Tipo_Alumno", "Modalidad",
        "Situacion_Academica", "Nombres", "Apellidos", "Email_Personal", "Email_Fen",
        "Email_laboral", "Celular", "Tel_laboral", "NomEmpresa", "CargoEmpresa",
    ];

    public static byte[] Create(IReadOnlyList<EnrolledStudentsDTO> rows) =>
        TextWorkbook.Create("Alumnos matriculados", Headers, rows.Select(row => new string?[]
        {
            row.StudentCode, row.EntrySemester, row.Rut, row.StudentType, row.Modality,
            row.AcademicSituation, row.Names, row.Surnames, row.PersonalEmail, row.FenEmail,
            row.WorkEmail, row.Mobile, row.WorkPhone, row.Company, row.Position,
        }));
}
