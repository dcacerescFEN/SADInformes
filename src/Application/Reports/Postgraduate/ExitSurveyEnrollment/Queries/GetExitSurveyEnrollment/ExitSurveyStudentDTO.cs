namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollment;
public sealed record ExitSurveyStudentDTO(string StudentType, string Modality, string PersonCode, string Name,
    string StudentCode, string EntrySemester, string ExitDate, string DegreeDate, bool Enrolled, string? Progress);
