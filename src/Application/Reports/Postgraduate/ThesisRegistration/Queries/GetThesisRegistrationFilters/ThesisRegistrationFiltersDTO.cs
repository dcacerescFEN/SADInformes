namespace Application.Reports.Postgraduate.ThesisRegistration.Queries.GetThesisRegistrationFilters;

public sealed record ThesisRegistrationFiltersDTO(
    ThesisRegistrationOptionDTO[] StudentTypes, string[] Modalities, string[] Years);
