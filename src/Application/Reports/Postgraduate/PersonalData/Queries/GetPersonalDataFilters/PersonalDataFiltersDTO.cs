namespace Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalDataFilters;

public sealed record PersonalDataFiltersDTO(
    PersonalDataOptionDTO[] StudentTypes, string[] Modalities, string[] Years);
