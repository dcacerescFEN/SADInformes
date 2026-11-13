namespace Application.Reports.AcademicEnrollment.CourseLoadReduction.Queries.GetCourseLoadReduction;

public sealed record CourseLoadReductionReportDTO(
    string Period,
    CourseLoadReductionRowDTO[] Rows,
    int Total);
