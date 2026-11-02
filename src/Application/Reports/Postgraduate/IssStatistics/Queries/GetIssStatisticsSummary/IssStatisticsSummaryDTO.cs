namespace Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsSummary;

public sealed record IssStatisticsSummaryDTO(
    IssStatisticsSampleDTO? Sample, IssStatisticsCategoryDTO[] Categories, string[] Observations);
