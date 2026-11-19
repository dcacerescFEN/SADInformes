using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatus;

public sealed record GetRequestsByStatus(string? Career, string? Status, string? Type)
    : IRequest<ApiResponse<RequestsByStatusReportDTO>>;

public sealed class GetRequestsByStatusHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetRequestsByStatus, ApiResponse<RequestsByStatusReportDTO>>
{
    public async Task<ApiResponse<RequestsByStatusReportDTO>> Handle(
        GetRequestsByStatus request, CancellationToken cancellationToken)
    {
        var career = NormalizeCareer(request.Career);
        var status = NormalizeStatus(request.Status);
        var type = request.Type?.Trim() ?? "67";
        if (type.Length is 0 or > 10)
            throw new ArgumentException("El tipo no es válido.");
        var data = await procedures.SpRec_SolicitudesXEstadoAsync(
            career, status, type, cancellationToken: cancellationToken) ?? [];
        var rows = data.Select(row => new RequestByStatusRowDTO(
            Text(row.Cod_Persona), Text(row.Cod_Alumno), Text(row.Nombre),
            FormatRequest(row.Cod_Solicitud), Text(row.Cod_Solicitud),
            Date(row.Fecha_Solicitud), Text(row.Tipo_Solicitud),
            Text(row.Situacion), Text(row.ComentarioValidacion),
            Text(row.Primero), Text(row.Ultimo))).ToArray();

        return responses.Success(new RequestsByStatusReportDTO(career, status, type, rows));
    }

    private static string NormalizeCareer(string? value)
    {
        var career = value?.Trim().ToUpperInvariant() ?? "IC";
        if (career is not ("IC" or "IICG" or "CA"))
            throw new ArgumentException("La carrera no es válida.");
        return career;
    }

    private static string NormalizeStatus(string? value)
    {
        var status = value?.Trim().ToUpperInvariant() ?? "NO IMPRESA";
        if (status is not ("NO IMPRESA" or "PENDIENTE"))
            throw new ArgumentException("El estado no es válido.");
        return status;
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Date(DateTime? value) =>
        value is null ? "" : value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    private static string FormatRequest(string? value)
    {
        var code = value?.Trim() ?? "";
        return code.Length > 5 ? code[..5] + "/" + code[5..] : code;
    }
}
