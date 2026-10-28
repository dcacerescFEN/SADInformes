namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;

public sealed record MassEnrollmentCandidateDTO(
    string StudentCode, string Name, string Rut, string EntrySemester,
    string Situation, string Reason, string Decree, bool Enrolled, string EntryType = "");
