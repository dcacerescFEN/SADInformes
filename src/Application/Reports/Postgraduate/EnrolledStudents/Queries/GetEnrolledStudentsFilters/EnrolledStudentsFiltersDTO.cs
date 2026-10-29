namespace Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudentsFilters;

public sealed record EnrolledStudentsFiltersDTO(
    EnrolledStudentsOptionDTO[] Modalities, string[] Years);
