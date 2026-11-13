namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoPage;

public sealed record StudentWithoutPhotoPageRowDTO(
    string Name,
    string Rut,
    string StudentCode,
    string StudentType,
    string Mobile,
    string MailFen,
    string PersonalMail);
