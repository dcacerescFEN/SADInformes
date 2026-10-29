namespace Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudents;

public sealed record EnrolledStudentsDTO(
    string StudentCode, string EntrySemester, string Rut, string StudentType, string Modality,
    string AcademicSituation, string Names, string Surnames, string PersonalEmail, string FenEmail,
    string WorkEmail, string Mobile, string WorkPhone, string Company, string Position);
