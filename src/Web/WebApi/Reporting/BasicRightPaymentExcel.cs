using Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPayments;

namespace WebApi.Reporting;

public static class BasicRightPaymentExcel
{
    private static readonly string[] Headers =
        ["Posición", "Nombre", "Rut", "Código Alumno", "Semestre de Ingreso", "Situación", "Promedio"];

    public static byte[] Create(IReadOnlyList<BasicRightPaymentRowDTO> rows) =>
        TextWorkbook.Create("Pago Derecho Básico", Headers, rows.Select(row => new string?[]
        {
            row.Position.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.Name, row.Rut, row.StudentCode, row.Entry, row.Situation,
            row.Average?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
        }));
}
