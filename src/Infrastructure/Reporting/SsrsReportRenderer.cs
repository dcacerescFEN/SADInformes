using Application.Common.Reporting;
using Application.Common.Interfaces;
using Infrastructure.Configuration;

namespace Infrastructure.Reporting;

/// <summary>Adaptador SSRS con clientes de vida corta administrados por la fábrica.</summary>
/// <param name="clientFactory">Fábrica que administra los handlers HTTP compartidos.</param>
public sealed class SsrsReportRenderer(IHttpClientFactory clientFactory, ReportingSettings settings) : IReportRenderer
{
    private readonly IHttpClientFactory _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

    /// <summary>Nombre del cliente con handler compartido y timeout por operación.</summary>
    public const string ClientName = "SsrsReports";

    /// <summary>Obtiene encabezados sin almacenar todo el cuerpo en memoria.</summary>
    /// <param name="request">Solicitud interna autorizada.</param>
    /// <param name="cancellationToken">Cancelación del envío y del cuerpo.</param>
    /// <returns>Resultado propietario de los recursos HTTP hasta su liberación.</returns>
    public async Task<IReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken cancellationToken)
    {
        SsrsReportRequestBuilder.Validate(request);
        cancellationToken.ThrowIfCancellationRequested();
        var target = SsrsReportRequestBuilder.Build(request, settings.BaseUrl, settings.Path);
        var format = SsrsReportRequestBuilder.GetFormat(request.Format);
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var deadlineToken = deadline.Token;
        deadline.CancelAfter(request.OperationTimeout);
        HttpClient? client = null;
        HttpResponseMessage? response = null;
        try
        {
            client = _clientFactory.CreateClient(ClientName);
            using var message = new HttpRequestMessage(HttpMethod.Get, target);
            response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new ReportRenderException((int)response.StatusCode);

            var stream = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
            return new SsrsReportRenderResult(response, stream, client, deadline, cancellationToken, format.ContentType, format.Extension);
        }
        catch (OperationCanceledException)
        {
            Cleanup();
            throw new OperationCanceledException("La operación de reporte fue cancelada.", null,
                cancellationToken.IsCancellationRequested ? cancellationToken : deadlineToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            Cleanup();
            throw new ReportRenderException();
        }
        catch
        {
            Cleanup();
            throw;
        }

        void Cleanup()
        {
            response?.Dispose();
            client?.Dispose();
            deadline.Dispose();
        }
    }
}
