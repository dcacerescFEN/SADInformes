namespace Application.Reports.Regulation.Ranking.Queries.GetRanking;

public sealed record RankingReportDTO(
    string Group,
    string Period,
    string Selection,
    string SortBy,
    string Direction,
    IReadOnlyList<RankingRowDTO> Rows);
