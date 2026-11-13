namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;

public sealed record StudentWithoutPhotoRowDTO(
    string Name,
    string Rut,
    string StudentCode,
    string StudentType,
    string Mobile,
    string MailFen,
    string PersonalMail);
