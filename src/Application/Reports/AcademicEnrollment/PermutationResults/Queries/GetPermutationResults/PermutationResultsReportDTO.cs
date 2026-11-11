namespace Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationResults;

public sealed record PermutationResultsReportDTO(string Period, PermutationResultRowDTO[] Rows, int Total);
