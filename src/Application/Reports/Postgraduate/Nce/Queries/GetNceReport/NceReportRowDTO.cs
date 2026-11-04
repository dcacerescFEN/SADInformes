namespace Application.Reports.Postgraduate.Nce.Queries.GetNceReport;

public sealed record NceReportRowDTO(
    string Id, string Date, string Cause, string StudentCode, string StudentType,
    string Modality, string AcademicSituation, string Reason, string Name);
