namespace Application.Common.Reporting;

/// <summary>Fallo seguro del servicio de reportes, sin URL, cuerpo ni excepción interna.</summary>
public sealed class ReportRenderException : Exception
{
    /// <summary>Crea un fallo sanitizado del transporte.</summary>
    /// <param name="statusCode">Código HTTP numérico, si se recibió una respuesta.</param>
    public ReportRenderException(int? statusCode = null)
        : base("No fue posible obtener el reporte solicitado.") => StatusCode = statusCode;

    /// <summary>Código HTTP opcional; no incluye descripción del servidor.</summary>
    public int? StatusCode { get; }
}
