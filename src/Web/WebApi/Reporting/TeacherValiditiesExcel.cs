using Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValidities;

namespace WebApi.Reporting;

public static class TeacherValiditiesExcel
{
    private static readonly string[] Headers =
    [
        "ID", "Año", "Periodo", "Cod_Catedra", "Seccion", "Catedra", "Tipo_Alumno",
        "Modalidad", "Promocion", "Cod_Persona", "Nombre", "Email1", "Horas_Dedicadas", "Vigencia",
    ];

    public static byte[] Create(IReadOnlyList<TeacherValiditiesDTO> rows) =>
        TextWorkbook.Create("Profesores", Headers, rows.Select(row => new string?[]
        {
            row.Id, row.Year, row.Period, row.CourseCode, row.Section, row.CourseName,
            row.StudentType, row.Modality, row.Promotion, row.PersonCode, row.Name, row.Email,
            row.DedicatedHours, row.Valid ? "Vigente" : "No Vigente",
        }));
}
