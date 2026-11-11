namespace Application.Reports.AcademicEnrollment.AutomaticMentionChanges.Queries.GetAutomaticMentionChanges;

public sealed record AutomaticMentionChangeRowDTO(
    string StudentCode,
    string Name,
    string Mention);
