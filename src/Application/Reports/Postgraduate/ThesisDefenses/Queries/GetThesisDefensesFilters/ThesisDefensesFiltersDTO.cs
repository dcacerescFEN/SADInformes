namespace Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefensesFilters;

public sealed record ThesisDefensesFiltersDTO(
    string[] Years, string[] StudentTypes, string[] Modalities);
