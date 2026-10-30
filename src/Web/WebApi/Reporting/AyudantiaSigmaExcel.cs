using Application.Reports.Postgraduate.AyudantiaSigma.Queries.GetAyudantiaSigma;

namespace WebApi.Reporting;

public static class AyudantiaSigmaExcel
{
    private static readonly string[] Headers =
    [
        "COD", "Nombre1", "Nombre2", "Apellido1", "Apellido2", "Rut", "Email",
        "Cod Cátedra", "Cátedra", "Sección", "Inicio", "Termino", "Año", "Periodo",
        "Tipo Alumno", "Modalidad", "Promocion", "Ccosto", "tipoPago", "Valor",
        "Existe sigma", "Fecha sigma", "Paso", "Flag", "id remu",
    ];

    public static byte[] Create(IReadOnlyList<AyudantiaSigmaDTO> rows) =>
        TextWorkbook.Create("ReporteAyudantiaSigma", Headers, rows.Select(row => new string?[]
        {
            row.Cod, row.Nombre1, row.Nombre2, row.Apellido1, row.Apellido2, row.Rut, row.Email,
            row.CourseCode, row.CourseName, row.Section, row.Start, row.End, row.Year, row.Period,
            row.StudentType, row.Modality, row.Promotion, row.CostCenter, row.PaymentType, row.Amount,
            row.ExistsInSigma, row.SigmaDate, row.Step, row.Flag, row.RemunerationId,
        }));
}
