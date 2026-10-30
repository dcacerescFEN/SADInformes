using Application.Reports.Postgraduate.EntrySurveyStatistics.Queries.GetEntrySurveyStatistics;

namespace WebApi.Reporting;

public static class EntrySurveyExcel
{
    private static readonly string[] Headers =
    [
        "Cod_Alumno", "Tipo_Alumno", "Modalidad", "Q1",
        "Q2_1", "Q2_2", "Q2_3", "Q2_4", "Q2_5", "Q2_6", "Q2_7", "Q2_8",
        "Q3_1", "Q3_2", "Q3_3", "Q3_4", "Q3_5", "Q3_6", "Q3_7",
        "Q4_1", "Q4_2", "Q4_3", "Q4_4", "Q4_5", "Q4_6", "Q4_7", "Q4_8",
        "Q5", "Q5_otro", "Q6", "Q7", "Q8", "Q9", "Q9_otro", "Q10",
        "Q11", "Q11_otro", "Q12", "Q13", "Q14", "Q15", "Q16", "Q17", "Fecha",
    ];

    public static byte[] Create(IReadOnlyList<EntrySurveyDTO> rows) =>
        TextWorkbook.Create("EncuestaEntradaPostgrado", Headers, rows.Select(row => new string?[]
        {
            row.CodAlumno, row.StudentType, row.Modality, row.Q1,
            row.Q2_1, row.Q2_2, row.Q2_3, row.Q2_4, row.Q2_5, row.Q2_6, row.Q2_7, row.Q2_8,
            row.Q3_1, row.Q3_2, row.Q3_3, row.Q3_4, row.Q3_5, row.Q3_6, row.Q3_7,
            row.Q4_1, row.Q4_2, row.Q4_3, row.Q4_4, row.Q4_5, row.Q4_6, row.Q4_7, row.Q4_8,
            row.Q5, row.Q5Otro, row.Q6, row.Q7, row.Q8, row.Q9, row.Q9Otro, row.Q10,
            row.Q11, row.Q11Otro, row.Q12, row.Q13, row.Q14, row.Q15, row.Q16, row.Q17, row.Fecha,
        }));
}
