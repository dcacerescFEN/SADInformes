namespace Application.Reports.Regulation.Ranking.Queries.GetRanking;

public sealed record RankingRowDTO(
    string StudentCode,
    string Name,
    string Decree,
    string DecreeDescription,
    string Average,
    string Promotion,
    string EntrySemester,
    int Ranking,
    int Total);
