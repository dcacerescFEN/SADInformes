using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdates;

namespace WebApi.Reporting;

/// <summary>Writes a real XLSX workbook; all cells are explicit text, never formulas.</summary>
public static class SadVtiExcel
{
    private const string Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly string[] Headers =
    [
        "Estado", "Descripcion", "Cod_Persona", "Cod_Alumno_Anterior", "Cod_Alumno",
        "NombreCompleto", "Tipo_Alumno", "Modalidad", "Cod_SitAcademica", "Matricula",
        "Fecha_Registro", "Mensaje_Error",
    ];

    public static byte[] Create(IReadOnlyList<SadVtiUpdateDTO> rows)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WritePart(zip, "[Content_Types].xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            WritePart(zip, "_rels/.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            WritePart(zip, "xl/workbook.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Auditoría SAD-VTI" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            WritePart(zip, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            using var stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open();
            using var xml = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
            xml.WriteStartDocument();
            xml.WriteStartElement("worksheet", Spreadsheet);
            xml.WriteStartElement("sheetData", Spreadsheet);
            WriteRow(xml, 1, Headers);
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                WriteRow(xml, index + 2,
                [
                    row.Estado, row.Descripcion, row.CodPersona, row.CodAlumnoAnterior, row.CodAlumno,
                    row.NombreCompleto, row.TipoAlumno, row.Modalidad, row.CodSitAcademica, row.Matricula,
                    row.FechaRegistro.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), row.MensajeError,
                ]);
            }
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }
        return buffer.ToArray();
    }

    private static void WritePart(ZipArchive zip, string path, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static void WriteRow(XmlWriter xml, int number, IReadOnlyList<string?> cells)
    {
        xml.WriteStartElement("row", Spreadsheet);
        xml.WriteAttributeString("r", number.ToString(CultureInfo.InvariantCulture));
        for (var index = 0; index < cells.Count; index++)
        {
            xml.WriteStartElement("c", Spreadsheet);
            xml.WriteAttributeString("r", $"{(char)('A' + index)}{number}");
            xml.WriteAttributeString("t", "inlineStr");
            xml.WriteStartElement("is", Spreadsheet);
            xml.WriteStartElement("t", Spreadsheet);
            xml.WriteAttributeString("xml", "space", null, "preserve");
            xml.WriteString(cells[index] ?? "");
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }
}
