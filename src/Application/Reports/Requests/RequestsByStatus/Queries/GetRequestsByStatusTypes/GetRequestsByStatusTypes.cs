using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatusTypes;

public sealed record GetRequestsByStatusTypes(string? Career, string? Status, string? TypeFilter)
    : IRequest<ApiResponse<RequestsByStatusTypesDTO>>;

public sealed class GetRequestsByStatusTypesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetRequestsByStatusTypes, ApiResponse<RequestsByStatusTypesDTO>>
{
    public async Task<ApiResponse<RequestsByStatusTypesDTO>> Handle(
        GetRequestsByStatusTypes request, CancellationToken cancellationToken)
    {
        var career = request.Career?.Trim().ToUpperInvariant() ?? "IC";
        if (career is not ("IC" or "IICG" or "CA"))
            throw new ArgumentException("La carrera no es válida.");

        var status = request.Status?.Trim().ToUpperInvariant() ?? "NO IMPRESA";
        if (status is not ("NO IMPRESA" or "PENDIENTE"))
            throw new ArgumentException("El estado no es válido.");

        var typeFilter = request.TypeFilter?.Trim().ToUpperInvariant() ?? "%";
        if (typeFilter.Length is 0 or > 10)
            throw new ArgumentException("El tipo no es válido.");

        var types = await procedures.SpRec_SolicitudesXEstado_TipoAsync(
            career, status, typeFilter, cancellationToken: cancellationToken) ?? [];
        var options = types
            .Where(row => !string.IsNullOrWhiteSpace(row.Cod_Tipo_Solicitud))
            .Select(row =>
            {
                var value = row.Cod_Tipo_Solicitud!.Trim();
                var mapped = value == "%" ? (typeFilter == "OTROS" ? "OTROS" : "%") : value;
                return new RequestStatusTypeOptionDTO(
                    mapped,
                    value == "%" ? "TODOS" : row.Tipo_Solicitud?.Trim() is { Length: > 0 } label
                        ? label : value);
            })
            .ToArray();

        return responses.Success(new RequestsByStatusTypesDTO(options));
    }
}
