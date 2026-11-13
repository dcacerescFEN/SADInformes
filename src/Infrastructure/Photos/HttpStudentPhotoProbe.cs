using System.Net;
using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Photos;

/// <summary>
/// Consulta la URL de fotos con HEAD, igual que el legado. Solo un 200 OK confirma
/// que la foto existe; cualquier otro estado, timeout o error de red se considera
/// foto faltante, que es el comportamiento de Utiles.ExisteURL.
/// </summary>
public sealed class HttpStudentPhotoProbe(
    IStudentPhotoSettings settings, HttpClient client, IMemoryCache cache) : IStudentPhotoProbe
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public async Task<bool> HasPhotoAsync(string personCode, CancellationToken cancellationToken)
    {
        var code = personCode.Trim();
        if (code.Length == 0) return false;
        var url = $"{settings.BaseUrl}{Uri.EscapeDataString(code)}.jpg";
        if (cache.TryGetValue<bool>(url, out var cached)) return cached;

        bool exists;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            exists = response.StatusCode == HttpStatusCode.OK;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
        {
            exists = false;
        }

        cache.Set(url, exists, CacheDuration);
        return exists;
    }
}
