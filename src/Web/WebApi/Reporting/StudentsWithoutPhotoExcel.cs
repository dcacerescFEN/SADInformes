using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;

namespace WebApi.Reporting;

public static class StudentsWithoutPhotoExcel
{
    private static readonly string[] Headers =
    [
        "Nombre", "Rut", "Cód. Alumno", "Tipo Alumno", "Celular", "Mail Fen", "Mail Personal",
    ];

    public static byte[] Create(IReadOnlyList<StudentWithoutPhotoRowDTO> rows) =>
        TextWorkbook.Create("Sin Fotos", Headers, rows.Select(row => new string?[]
        {
            row.Name, row.Rut, row.StudentCode, row.StudentType, row.Mobile, row.MailFen, row.PersonalMail,
        }));
}
