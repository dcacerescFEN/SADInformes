namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Commands.CreateExitSurveyEnrollment;
public sealed record CreateExitSurveyEnrollmentRequestDTO(string? StudentType, string? Modality, string? Year,
    string[]? StudentCodes, bool Confirmed);
