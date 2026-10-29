namespace Application.Common.Reporting;

/// <summary>Formatos de salida admitidos, independientes de SSRS.</summary>
public enum ReportFormat
{
    /// <summary>Documento PDF.</summary>
    PDF,
    /// <summary>Libro de cálculo Open XML.</summary>
    EXCEL,
    /// <summary>Documento de texto Open XML.</summary>
    WORD,
    /// <summary>Valores separados por comas.</summary>
    CSV,
}
