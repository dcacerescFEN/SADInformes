using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Reporting;

namespace Application.Reports.Requests.RequestsByStatus.Queries.GetRequestPrintDetail;

public sealed record GetRequestPrintDetail(string? RequestCode) : IRequest<IReportRenderResult>;

public sealed class GetRequestPrintDetailHandler(IReportRenderer renderer)
    : IRequestHandler<GetRequestPrintDetail, IReportRenderResult>
{
    public Task<IReportRenderResult> Handle(GetRequestPrintDetail request, CancellationToken ct)
    {
        // El código solo identifica la solicitud; el RDL resuelve sus datos.
        var code = request.RequestCode?.Replace("/", "", StringComparison.Ordinal).Trim() ?? "";
        if (code.Length is 0 or > 20 || !code.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException("La solicitud no es válida.");
        return renderer.RenderAsync(new ReportRenderRequest("SolicitudImpresion",
            new Dictionary<string, string> { ["CodSolicitud"] = code },
            ReportFormat.PDF, TimeSpan.FromSeconds(120)), ct);
    }
}
