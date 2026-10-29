namespace Application.Reports.Postgraduate.StudentRequests.Queries.GetStudentRequestsFilters;
public sealed record StudentRequestsFiltersDTO(StudentRequestsOptionDTO[] StudentTypes, string[] Modalities, string[] Cohorts, string[] States);
