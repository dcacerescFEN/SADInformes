namespace Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRankingFilters;

public sealed record WeightedRankingFiltersDTO(
    WeightedRankingOptionDTO[] StudentTypes, string[] Modalities, string[] Years);
