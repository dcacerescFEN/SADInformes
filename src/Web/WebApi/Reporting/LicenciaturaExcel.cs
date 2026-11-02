using Application.Reports.Postgraduate.Licenciatura.Queries.GetLicenciatura;

namespace WebApi.Reporting;

public static class LicenciaturaExcel
{
    private static readonly string[] Headers =
        ["Código Alumno", "Nombre", "Nombre Carrera", "Grado", "Estado"];

    public static byte[] Create(IReadOnlyList<LicenciaturaRowDTO> rows) =>
        TextWorkbook.Create("Licenciatura", Headers, rows.Select(row => new string?[]
        {
            row.StudentCode, row.Name, row.Career, row.Degree, row.Status,
        }));
}
