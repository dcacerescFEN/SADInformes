using Application.Reports.Postgraduate.Nce.Queries.GetNceReport;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace WebApi.Reporting;

/// <summary>Genera el informe NCE en PDF con el mismo contenido que el XLSX.</summary>
public static class NceReportPdf
{
    private static readonly string[] Headers =
    [
        "Fila", "ID", "Fecha", "Causal", "Código Alumno", "Tipo Alumno", "Modalidad",
        "Situación Académica", "Motivo", "Nombre",
    ];
    private static readonly double[] Widths = [1.2, 4.4, 2.8, 4.3, 2.8, 2.2, 2.2, 3.2, 4.0, 5.2];

    public static byte[] Create(IReadOnlyList<NceReportRowDTO> rows)
    {
        GlobalFontSettings.FontResolver ??= new CarlitoFontResolver();
        var document = new Document();
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.Legal;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.4);

        var title = section.AddParagraph("Informe Notificaciones Causal de Eliminación");
        title.Format.Font.Name = "Carlito";
        title.Format.Font.Size = 14;
        title.Format.Font.Bold = true;
        title.Format.SpaceAfter = Unit.FromCentimeter(0.2);
        var generated = section.AddParagraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}");
        generated.Format.Font.Name = "Carlito";
        generated.Format.Font.Size = 8;
        generated.Format.SpaceAfter = Unit.FromCentimeter(0.4);

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = Colors.Gray;
        foreach (var width in Widths) table.AddColumn(Unit.FromCentimeter(width));

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Color.FromRgb(45, 65, 84);
        header.Format.Font.Name = "Carlito";
        header.Format.Font.Size = 7.5;
        header.Format.Font.Bold = true;
        header.Format.Font.Color = Colors.White;
        header.Format.Alignment = ParagraphAlignment.Center;
        for (var index = 0; index < Headers.Length; index++) header.Cells[index].AddParagraph(Headers[index]);

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var tableRow = table.AddRow();
            if (index % 2 == 1) tableRow.Shading.Color = Color.FromRgb(243, 243, 243);
            tableRow.Format.Font.Name = "Carlito";
            tableRow.Format.Font.Size = 7;
            tableRow.Format.LeftIndent = 0;
            var values = new[]
            {
                (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), row.Id, row.Date,
                row.Cause, row.StudentCode, row.StudentType, row.Modality, row.AcademicSituation,
                row.Reason, row.Name,
            };
            for (var cellIndex = 0; cellIndex < values.Length; cellIndex++)
                tableRow.Cells[cellIndex].AddParagraph(values[cellIndex]);
        }

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var output = new MemoryStream();
        renderer.PdfDocument.Save(output, false);
        return output.ToArray();
    }
}
