namespace Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationResultsByCourse;

public sealed record PostulationResultsReportDTO(
    string Period,
    string Origin,
    PostulationResultRowDTO[] Rows,
    int Total);
