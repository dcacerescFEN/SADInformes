namespace Application.Common.Reporting;

/// <summary>Resultado con recursos propios; no expone el stream de transporte.</summary>
public interface IReportRenderResult : IDisposable, IAsyncDisposable
{
    /// <summary>Tipo MIME del formato solicitado.</summary>
    string ContentType { get; }
    /// <summary>Extensión del archivo, sin punto inicial.</summary>
    string FileExtension { get; }
    /// <summary>Copia el cuerpo una sola vez; no admite copias concurrentes ni reintentos.</summary>
    /// <param name="destination">Destino del consumidor; nunca se libera por este resultado.</param>
    /// <param name="cancellationToken">Cancelación adicional del consumidor.</param>
    /// <returns>Tarea de copia dentro del plazo original, incluido tiempo previo a copiar.</returns>
    /// <remarks>Debe liberarse tras uso. Un fallo de copia libera los recursos propios.</remarks>
    Task CopyToAsync(Stream destination, CancellationToken cancellationToken);
}
