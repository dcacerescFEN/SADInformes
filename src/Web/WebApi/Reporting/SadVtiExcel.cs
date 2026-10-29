using System.Globalization;
using Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdates;

namespace WebApi.Reporting;

/// <summary>Writes a real XLSX workbook; all cells are explicit text, never formulas.</summary>
public static class SadVtiExcel
{
    private static readonly string[] Headers =
    [
        "Estado", "Descripcion", "Cod_Persona", "Cod_Alumno_Anterior", "Cod_Alumno",
        "NombreCompleto", "Tipo_Alumno", "Modalidad", "Cod_SitAcademica", "Matricula",
        "Fecha_Registro", "Mensaje_Error",
    ];

    public static byte[] Create(IReadOnlyList<SadVtiUpdateDTO> rows) =>
        TextWorkbook.Create("Auditoría SAD-VTI", Headers, rows.Select(row => new string?[]
        {
            row.Estado, row.Descripcion, row.CodPersona, row.CodAlumnoAnterior, row.CodAlumno,
            row.NombreCompleto, row.TipoAlumno, row.Modalidad, row.CodSitAcademica, row.Matricula,
            row.FechaRegistro.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
            row.MensajeError,
        }));
}
