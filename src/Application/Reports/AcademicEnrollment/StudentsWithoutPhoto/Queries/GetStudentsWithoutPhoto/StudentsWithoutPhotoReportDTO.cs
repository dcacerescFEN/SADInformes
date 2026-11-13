namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;

public sealed record StudentsWithoutPhotoReportDTO(
    StudentWithoutPhotoRowDTO[] Rows,
    int Total);
