namespace Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingFilters;

public sealed record AutomaticProcessingRequestsFiltersDTO(
    IReadOnlyList<string> Periods,
    IReadOnlyList<RequestOptionDTO> Careers,
    IReadOnlyList<RequestOptionDTO> Types,
    IReadOnlyList<RequestOptionDTO> States);

public sealed record RequestOptionDTO(string Value, string Text);
