using Application.Common.Reporting;

namespace Application.Common.Interfaces;

/// <summary>Genera reportes sin exponer detalles del transporte.</summary>
public interface IReportRenderer
{
    /// <summary>Obtiene un resultado cuya propiedad se transfiere al consumidor.</summary>
    /// <param name="request">Reporte y parámetros previamente autorizados por Application.</param>
    /// <param name="cancellationToken">Cancelación vigente durante envío y consumo del cuerpo.</param>
    /// <returns>Resultado que el consumidor debe liberar, incluso si no copia el cuerpo.</returns>
    Task<IReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken cancellationToken);
}
