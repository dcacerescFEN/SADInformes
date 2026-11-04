namespace Application.Reports.Postgraduate.Nce.Queries.GetNceReport;

public sealed record NceReportDTO(NceReportRowDTO[] Rows, int Total);
