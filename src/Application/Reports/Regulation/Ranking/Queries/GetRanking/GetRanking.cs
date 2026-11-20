using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Regulation.Ranking.Common;
using Domain.Common;

namespace Application.Reports.Regulation.Ranking.Queries.GetRanking;

public sealed record GetRanking(
    string? Group, string? Period, string? Selection, string? SortBy, string? Direction)
    : IRequest<ApiResponse<RankingReportDTO>>;

public sealed class GetRankingHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetRanking, ApiResponse<RankingReportDTO>>
{
    public async Task<ApiResponse<RankingReportDTO>> Handle(
        GetRanking request, CancellationToken cancellationToken)
    {
        var group = RankingRules.Group(request.Group);
        var period = RankingRules.Period(request.Period);
        var selection = RankingRules.Selection(request.Selection);
        var sortBy = RankingRules.SortBy(request.SortBy);
        var direction = RankingRules.Direction(request.Direction);

        var data = await procedures.SpRec_RankingEgresoXGrupoAsync(
            group, period, selection, sortBy, direction,
            cancellationToken: cancellationToken) ?? [];

        var rows = new List<RankingRowDTO>(data.Count);
        foreach (var row in data)
        {
            rows.Add(new RankingRowDTO(
                Text(row.Cod_Alumno), Text(row.Nombre), Text(row.Decreto), Text(row.Decreto2),
                RankingRules.Average(row.Promedio), Text(row.Promocion), Text(row.Sem_Ingreso),
                row.Ranking ?? 0, row.Total ?? 0));
        }

        return responses.Success(
            new RankingReportDTO(group, period, selection, sortBy, direction, rows));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
