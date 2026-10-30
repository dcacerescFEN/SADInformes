namespace Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalData;

public sealed record PersonalDataDTO(
    string IdAlumno, string StudentCode, string EntrySemester, string PersonCode, string StudentType,
    string Modality, string AcademicSituation, string Reason, string Names, string Surnames,
    string PersonalEmail, string FenEmail, string WorkEmail, string Mobile, string WorkPhone,
    string Company, string Position, string BirthDate, string Nationality, string Sex,
    string PreviousDegree, string OriginUniversity, string ResolutionDate, string ExamDate,
    string LastCoursePeriod, string ExamPeriod);
