namespace Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRanking;

public sealed record WeightedRankingDTO(
    int Position, string Name, string Rut, string StudentCode, string EntrySemester,
    string Situation, string WeightedAverage, string Percentile);
