namespace Application.Reports.Postgraduate.IssStatistics.Queries.GetIssStatisticsSummary;

public sealed record IssStatisticsSampleDTO(
    string CourseCode, string Section, string Period, int? Enrolled, int? Answered,
    double? Percentage, int? Missing);
