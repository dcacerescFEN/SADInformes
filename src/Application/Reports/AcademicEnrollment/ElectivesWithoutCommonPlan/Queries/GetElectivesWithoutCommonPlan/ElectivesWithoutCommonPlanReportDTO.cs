namespace Application.Reports.AcademicEnrollment.ElectivesWithoutCommonPlan.Queries.GetElectivesWithoutCommonPlan;

public sealed record ElectivesWithoutCommonPlanRowDTO(
    string Decree,
    string CourseCode,
    string CourseName);

public sealed record ElectivesWithoutCommonPlanReportDTO(
    IReadOnlyList<ElectivesWithoutCommonPlanRowDTO> Rows,
    int Total);
