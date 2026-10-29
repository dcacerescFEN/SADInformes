using System.Globalization;
using Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRanking;

namespace WebApi.Reporting;

public static class WeightedRankingExcel
{
    private static readonly string[] Headers =
    [
        "Posición", "Nombre", "Rut", "Código Alumno", "Semestre de Ingreso",
        "Situación", "Promedio", "Percentil",
    ];

    public static byte[] Create(IReadOnlyList<WeightedRankingDTO> rows) =>
        TextWorkbook.Create("Ranking", Headers, rows.Select(row => new string?[]
        {
            row.Position.ToString(CultureInfo.InvariantCulture), row.Name, row.Rut, row.StudentCode,
            row.EntrySemester, row.Situation, row.WeightedAverage, row.Percentile,
        }));
}
