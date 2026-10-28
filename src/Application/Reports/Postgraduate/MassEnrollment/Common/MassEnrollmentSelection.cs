namespace Application.Reports.Postgraduate.MassEnrollment.Common;

public sealed record MassEnrollmentSelection(
    string StudentType, string Modality, string Cohort, string Period, string Course);
