namespace Application.Reports.AcademicEnrollment.FailedMandatoryNotEnrolled.Queries.GetFailedMandatoryNotEnrolled;

public sealed record FailedMandatoryNotEnrolledReportDTO(
    string Period,
    FailedMandatoryNotEnrolledRowDTO[] Rows,
    int Total);
