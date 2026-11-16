namespace Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingRequests;

public sealed record AutomaticProcessingRequestsReportDTO(
    string Period,
    string Career,
    string Type,
    string Status,
    IReadOnlyList<AutomaticProcessingRequestRowDTO> Rows);

public sealed record AutomaticProcessingRequestRowDTO(
    string Name,
    string StudentCode,
    string RequestCode,
    string Date,
    string Type,
    string Requested,
    string AdministrativeProcessing,
    string Result,
    string ProcessDate);
