namespace Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRanking;

public sealed record WeightedRankingReportDTO(WeightedRankingDTO[] Rows, int Total);
