namespace Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatus;

public sealed record RequestsByStatusReportDTO(
    string Career,
    string Status,
    string Type,
    IReadOnlyList<RequestByStatusRowDTO> Rows);

public sealed record RequestByStatusRowDTO(
    string PersonCode,
    string StudentCode,
    string Name,
    string RequestCode,
    string RawRequestCode,
    string Date,
    string Type,
    string Situation,
    string Comment,
    string FirstRequest,
    string LastRequest);
