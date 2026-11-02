namespace Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReportFilters;

public sealed record AfeReportFiltersDTO(
    AfeReportOptionDTO[] Periods, AfeReportOptionDTO[] StudentTypes,
    AfeReportOptionDTO[] Modalities, AfeReportOptionDTO[] Courses);
