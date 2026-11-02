using Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReport;

namespace WebApi.Reporting;

public static class AfeReportExcel
{
    private static readonly string[] Headers =
    [
        "Avance Afe", "Nombre Avance", "Fecha Creación", "Fecha Entrega Programada",
        "Nombre Profesor", "Catedra", "Nombre Alumno", "Grupo", "Fecha Entrega Real",
        "Evaluación", "Fecha Evaluación", "Obs. Evaluación",
    ];

    public static byte[] Create(IReadOnlyList<AfeReportRowDTO> rows) =>
        TextWorkbook.Create("Avances AFE", Headers, rows.Select(row => new string?[]
        {
            row.Advance, row.Title, row.CreatedAt, row.DueAt, row.Teacher, row.Course, row.Student,
            row.Group, row.DeliveredAt, row.Grade, row.EvaluatedAt, row.EvaluationObservation,
        }));
}
