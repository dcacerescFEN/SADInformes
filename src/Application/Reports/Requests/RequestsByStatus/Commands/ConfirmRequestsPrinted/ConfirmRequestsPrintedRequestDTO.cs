namespace Application.Reports.Requests.RequestsByStatus.Commands.ConfirmRequestsPrinted;

public sealed record ConfirmRequestsPrintedRequestDTO(IReadOnlyList<string>? RequestCodes);
