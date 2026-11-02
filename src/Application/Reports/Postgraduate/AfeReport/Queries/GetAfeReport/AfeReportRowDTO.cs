namespace Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReport;

public sealed record AfeReportRowDTO(
    string Advance, string Title, string CreatedAt, string DueAt, string Teacher, string Course,
    string Student, string Group, string DeliveredAt, string Grade, string EvaluatedAt,
    string EvaluationObservation);
