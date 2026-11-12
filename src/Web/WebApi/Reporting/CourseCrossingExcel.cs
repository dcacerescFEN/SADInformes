using Application.Reports.AcademicEnrollment.CourseCrossing.Queries.GetCourseCrossing;

namespace WebApi.Reporting;

public static class CourseCrossingExcel
{
    private static readonly string[] Headers =
    [
        "Curso 1", "Cátedra 1", "Curso 2", "Cátedra 2",
        "Topes IC", "Topes IICG", "Topes Otros", "Topes Total",
    ];

    public static byte[] Create(IReadOnlyList<CourseCrossingRowDTO> rows) =>
        TextWorkbook.Create("Cruce de Cursos", Headers, rows.Select(row => new string?[]
        {
            row.Course1, row.CourseName1, row.Course2, row.CourseName2,
            row.IcLimit, row.IicgLimit, row.OtherLimit, row.TotalLimit,
        }));
}
