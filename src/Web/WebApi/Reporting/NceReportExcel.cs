using Application.Reports.Postgraduate.Nce.Queries.GetNceReport;

namespace WebApi.Reporting;

public static class NceReportExcel
{
    private static readonly string[] Headers =
    [
        "Fila", "ID", "Fecha", "Causal", "Código Alumno", "Tipo Alumno", "Modalidad",
        "Situación Académica", "Motivo", "Nombre",
    ];

    public static byte[] Create(IReadOnlyList<NceReportRowDTO> rows) =>
        TextWorkbook.Create("Notificaciones NCE", Headers, rows.Select((row, index) => new string?[]
        {
            (index + 1).ToString(), row.Id, row.Date, row.Cause, row.StudentCode, row.StudentType,
            row.Modality, row.AcademicSituation, row.Reason, row.Name,
        }));
}
