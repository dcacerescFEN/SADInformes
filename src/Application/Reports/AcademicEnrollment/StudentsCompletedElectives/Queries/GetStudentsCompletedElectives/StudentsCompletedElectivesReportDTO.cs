namespace Application.Reports.AcademicEnrollment.StudentsCompletedElectives.Queries.GetStudentsCompletedElectives;

public sealed record StudentsCompletedElectivesReportDTO(
    string Period,
    StudentCompletedElectiveRowDTO[] Rows,
    int Total);
