namespace Application.Common.Reporting;

/// <summary>Solicitud interna; no sustituye la selección ni autorización de negocio.</summary>
/// <param name="ReportName">Nombre del reporte autorizado.</param>
/// <param name="Parameters">Parámetros de negocio, conservando sus nombres exactos.</param>
/// <param name="Format">Formato de salida solicitado.</param>
/// <param name="OperationTimeout">Plazo explícito desde el envío hasta finalizar la copia.</param>
/// <param name="PageSize">Tamaño de hoja autorizado por el servidor, si el reporte lo requiere.</param>
public sealed record ReportRenderRequest(
    string ReportName,
    IReadOnlyDictionary<string, string> Parameters,
    ReportFormat Format,
    TimeSpan OperationTimeout,
    ReportPageSize? PageSize = null);
