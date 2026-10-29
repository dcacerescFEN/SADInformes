using Application.Common.Reporting;

namespace Infrastructure.Reporting;

internal sealed class SsrsReportRenderResult(
    HttpResponseMessage response,
    Stream source,
    HttpClient client,
    CancellationTokenSource deadline,
    CancellationToken originalToken,
    string contentType,
    string extension) : IReportRenderResult
{
    private readonly object _gate = new();
    private readonly CancellationToken _deadlineToken = deadline.Token;
    private bool _disposed;
    private bool _copyStarted;

    public string ContentType => contentType;
    public string FileExtension => extension;

    public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException("El destino no permite escritura.", nameof(destination));
        CancellationTokenSource copyCancellation;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_copyStarted) throw new InvalidOperationException("El resultado admite una única copia.");
            _copyStarted = true;
            copyCancellation = CancellationTokenSource.CreateLinkedTokenSource(_deadlineToken, cancellationToken);
        }

        using (copyCancellation)
        {
            try
            {
                var buffer = new byte[81920];
                while (true)
                {
                    copyCancellation.Token.ThrowIfCancellationRequested();
                    int count;
                    try
                    {
                        count = await source.ReadAsync(buffer, copyCancellation.Token).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (exception is HttpRequestException or IOException)
                    {
                        throw new ReportRenderException();
                    }
                    if (count == 0) break;
                    // Fallos de escritura pertenecen al consumidor, no al servicio de reportes.
                    await destination.WriteAsync(buffer.AsMemory(0, count), copyCancellation.Token).ConfigureAwait(false);
                }
                copyCancellation.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Dispose();
                throw new OperationCanceledException("La operación de reporte fue cancelada.", null,
                    cancellationToken.IsCancellationRequested ? cancellationToken
                    : originalToken.IsCancellationRequested ? originalToken : _deadlineToken);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            deadline.Cancel();
            // HttpContent libera su stream; evitar una segunda liberación explícita del mismo.
            response.Dispose();
            client.Dispose();
            deadline.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
