namespace Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatusTypes;

public sealed record RequestsByStatusTypesDTO(IReadOnlyList<RequestStatusTypeOptionDTO> Types);

public sealed record RequestStatusTypeOptionDTO(string Value, string Text);
