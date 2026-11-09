namespace Application.Reports.InscripcionAcademica.ElectivesWithoutCommonPlan.Queries.GetElectivesWithoutCommonPlan;

public sealed record ElectivesWithoutCommonPlanRowDTO(
    string Decree,
    string CourseCode,
    string CourseName);

public sealed record ElectivesWithoutCommonPlanReportDTO(
    IReadOnlyList<ElectivesWithoutCommonPlanRowDTO> Rows,
    int Total);
