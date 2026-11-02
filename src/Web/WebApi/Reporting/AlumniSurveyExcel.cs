using Application.Reports.Postgraduate.AlumniSurveyStatistics.Queries.GetAlumniSurvey;

namespace WebApi.Reporting;

public static class AlumniSurveyExcel
{
    private static readonly string[] Headers =
    [
        "Cod_Alumno", "P1", "P1_1", "P1_2", "P1_3", "P1_4", "P2", "P2_1", "P2_2", "P2_3",
        "P2_4", "P3_1", "P3_2", "P3_3", "P3_4", "P4", "P5", "P6", "P7", "P8_1", "P8_2",
        "P9", "P10", "P10_1", "P10_2", "P10_3", "P10_4", "P11", "P12", "P13", "P14", "Fecha",
    ];

    public static byte[] Create(IReadOnlyList<AlumniSurveyDTO> rows) =>
        TextWorkbook.Create("Encuesta Alumni", Headers, rows.Select(row => new string?[]
        {
            row.CodAlumno, row.P1, row.P1_1, row.P1_2, row.P1_3, row.P1_4, row.P2, row.P2_1,
            row.P2_2, row.P2_3, row.P2_4, row.P3_1, row.P3_2, row.P3_3, row.P3_4, row.P4,
            row.P5, row.P6, row.P7, row.P8_1, row.P8_2, row.P9, row.P10, row.P10_1, row.P10_2,
            row.P10_3, row.P10_4, row.P11, row.P12, row.P13, row.P14, row.Fecha,
        }));
}
