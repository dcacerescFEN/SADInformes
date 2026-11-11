namespace Application.Reports.AcademicEnrollment.AutomaticMentionChanges.Queries.GetAutomaticMentionChanges;

public sealed record AutomaticMentionChangesReportDTO(
    string Period,
    AutomaticMentionChangeRowDTO[] Rows,
    int Total);
