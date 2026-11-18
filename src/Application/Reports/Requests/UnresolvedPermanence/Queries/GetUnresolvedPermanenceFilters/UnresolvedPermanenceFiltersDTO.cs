namespace Application.Reports.Requests.UnresolvedPermanence.Queries.GetUnresolvedPermanenceFilters;

public sealed record UnresolvedPermanenceFiltersDTO(
    IReadOnlyList<UnresolvedPermanenceOptionDTO> Careers);

public sealed record UnresolvedPermanenceOptionDTO(string Value, string Text);
