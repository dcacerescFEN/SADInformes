using Application.Reports.AcademicEnrollment.CourseLoadReduction.Queries.GetCourseLoadReduction;

namespace WebApi.Reporting;

public static class CourseLoadReductionExcel
{
    private static readonly string[] Headers =
    [
        "Período", "Cátedra", "Sección", "Nombre Cátedra", "Cód. Alumno",
        "Cód. Persona", "Decreto", "Nombre", "Origen", "Tipo Movimiento", "Estado",
    ];

    public static byte[] Create(IReadOnlyList<CourseLoadReductionRowDTO> rows) =>
        TextWorkbook.Create("Reducción de Carga", Headers, rows.Select(row => new string?[]
        {
            row.Period, row.CourseCode, row.Section, row.CourseName, row.StudentCode,
            row.PersonCode, row.Decree, row.Name, row.Origin, row.MovementType, row.State,
        }));
}
