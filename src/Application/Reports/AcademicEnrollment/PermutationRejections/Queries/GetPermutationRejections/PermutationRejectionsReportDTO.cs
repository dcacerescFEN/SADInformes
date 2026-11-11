namespace Application.Reports.AcademicEnrollment.PermutationRejections.Queries.GetPermutationRejections;

public sealed record PermutationRejectionsReportDTO(
    string Period,
    PermutationRejectionRowDTO[] Rows,
    int Total);
