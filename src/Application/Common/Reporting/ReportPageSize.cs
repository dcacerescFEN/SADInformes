namespace Application.Common.Reporting;

/// <summary>Tamaño de hoja autorizado para SSRS, expresado como dimensiones físicas.</summary>
/// <param name="Width">Ancho de página aceptado por SSRS, por ejemplo 21.59cm.</param>
/// <param name="Height">Alto de página aceptado por SSRS, por ejemplo 27.94cm.</param>
public sealed record ReportPageSize(string Width, string Height);
