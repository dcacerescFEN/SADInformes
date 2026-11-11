namespace Application.Reports.AcademicEnrollment.EnrolledWithoutRequirements.Queries.GetEnrolledWithoutRequirements;

public sealed record EnrolledWithoutRequirementsReportDTO(
    string Period,
    EnrolledWithoutRequirementsRowDTO[] Rows,
    int Total);
