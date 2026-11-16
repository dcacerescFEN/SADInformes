using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingRequests;

public sealed record GetAutomaticProcessingRequests(
    string? Period, string? Career, string? Type, string? Status)
    : IRequest<ApiResponse<AutomaticProcessingRequestsReportDTO>>;

public sealed class GetAutomaticProcessingRequestsHandler(
    ISadContext sad, ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetAutomaticProcessingRequests, ApiResponse<AutomaticProcessingRequestsReportDTO>>
{
    private static readonly string[] AutomaticTypes = ["2", "8", "10"];

    public async Task<ApiResponse<AutomaticProcessingRequestsReportDTO>> Handle(
        GetAutomaticProcessingRequests request, CancellationToken cancellationToken)
    {
        var period = request.Period?.Trim() ?? "";
        if (period.Length == 0)
            period = await sad.Solicitudes89.AsNoTracking()
                .Where(row => row.PeriodoSolicitud != null && AutomaticTypes.Contains(row.TipoSolicitud!))
                .OrderByDescending(row => row.PeriodoSolicitud)
                .Select(row => row.PeriodoSolicitud!)
                .FirstOrDefaultAsync(cancellationToken) ?? "";
        if (period.Length is 0 or > 5) throw new ArgumentException("El período no es válido.");

        var career = request.Career?.Trim().ToUpperInvariant() ?? "";
        if (career is not ("" or "IC" or "IICG" or "CA"))
            throw new ArgumentException("La carrera no es válida.");

        var type = request.Type?.Trim().ToUpperInvariant() ?? "";
        var status = request.Status?.Trim().ToUpperInvariant() ?? "";
        if (status.Length > 255 || type.Length > 255)
            throw new ArgumentException("El filtro no es válido.");

        var data = await procedures.SpRec_SolicitudesProcesadasAutomaticamenteSadAsync(
            career, period, type, status, cancellationToken: cancellationToken) ?? [];
        var rows = data.Select(row => new AutomaticProcessingRequestRowDTO(
            Text(row.Nombre), Text(row.Cod_Alumno), Text(row.Cod_Solicitud),
            Date(row.Fecha_Solicitud), Text(row.Tipo), Text(row.Solicitado),
            Text(row.Tramitacion_Administrativa), Text(row.Resultado),
            Date(row.FechaAut))).ToArray();

        return responses.Success(new AutomaticProcessingRequestsReportDTO(
            period, career, type, status, rows));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Date(DateTime? value) =>
        value is null ? "" : value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
