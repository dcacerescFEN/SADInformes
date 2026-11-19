namespace Application.Reports.Requests.RequestsByStatus.Commands.UpdateRequestValidationComment;

public sealed record UpdateRequestValidationCommentRequestDTO(string? RequestCode, string? Comment);
