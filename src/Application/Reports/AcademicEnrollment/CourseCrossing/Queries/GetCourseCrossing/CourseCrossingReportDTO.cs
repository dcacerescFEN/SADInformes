namespace Application.Reports.AcademicEnrollment.CourseCrossing.Queries.GetCourseCrossing;

public sealed record CourseCrossingReportDTO(
    string Period,
    CourseCrossingRowDTO[] Rows,
    int Total);
