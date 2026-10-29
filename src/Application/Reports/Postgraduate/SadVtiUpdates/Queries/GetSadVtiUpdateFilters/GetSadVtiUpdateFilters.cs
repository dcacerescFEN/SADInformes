using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdateFilters;

public sealed record GetSadVtiUpdateFilters : IRequest<ApiResponse<SadVtiUpdateFiltersDTO>>;
public sealed class GetSadVtiUpdateFiltersHandler(ISadContext context, IApiResponseService responses)
    : IRequestHandler<GetSadVtiUpdateFilters, ApiResponse<SadVtiUpdateFiltersDTO>>
{
    public async Task<ApiResponse<SadVtiUpdateFiltersDTO>> Handle(GetSadVtiUpdateFilters request, CancellationToken ct)
    {
        var states = await context.AudiEstadoPostgradoAlumnosGuia.AsNoTracking().OrderBy(row => row.Id)
            .Select(row => new SadVtiStateDTO(row.Estado, row.Descripcion)).ToArrayAsync(ct);
        return responses.Success(new SadVtiUpdateFiltersDTO([new("-1", "Todos"), .. states.Where(row => row.Value != "-1")]));
    }
}
