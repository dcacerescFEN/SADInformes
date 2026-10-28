namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;

public sealed record MassEnrollmentFiltersDTO(
    IReadOnlyList<MassEnrollmentOptionDTO> StudentTypes, IReadOnlyList<string> Modalities,
    IReadOnlyList<string> Cohorts, IReadOnlyList<MassEnrollmentOptionDTO> Periods,
    IReadOnlyList<MassEnrollmentOptionDTO> Courses);
