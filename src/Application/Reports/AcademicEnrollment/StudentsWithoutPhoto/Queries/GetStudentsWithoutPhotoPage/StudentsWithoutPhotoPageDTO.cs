namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoPage;

public sealed record StudentsWithoutPhotoPageDTO(
    StudentWithoutPhotoPageRowDTO[] Rows,
    int Page,
    bool HasMore,
    int Scanned);
