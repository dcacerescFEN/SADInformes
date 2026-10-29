using Application.Common.Reporting;

namespace WebApi.Reporting;

/// <summary>Posee el reporte durante toda la ejecución de la descarga HTTP.</summary>
/// <param name="rendered">Reporte cuyos recursos se liberan al finalizar.</param>
/// <param name="fileName">Nombre de descarga sin extensión.</param>
public sealed class ReportDownloadResult(IReportRenderResult rendered, string fileName) : IResult
{
    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        try
        {
            var result = Results.Stream(
                destination => rendered.CopyToAsync(destination, httpContext.RequestAborted),
                rendered.ContentType,
                $"{fileName}.{rendered.FileExtension}");
            await result.ExecuteAsync(httpContext);
        }
        finally
        {
            await rendered.DisposeAsync();
        }
    }
}
