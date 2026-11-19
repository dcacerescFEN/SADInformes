using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Requests.RequestsByStatus.Commands.UpdateRequestValidationComment;

public sealed record UpdateRequestValidationComment(string? RequestCode, string? Comment)
    : IRequest<ApiResponse<bool>>;

public sealed class UpdateRequestValidationCommentHandler(
    ISadContext sad, IApiResponseService responses)
    : IRequestHandler<UpdateRequestValidationComment, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        UpdateRequestValidationComment request, CancellationToken cancellationToken)
    {
        var code = request.RequestCode?.Trim() ?? "";
        if (code.Length is 0 or > 20) throw new ArgumentException("La solicitud no es válida.");
        var comment = request.Comment?.Trim() ?? "";
        if (comment.Length > 400) throw new ArgumentException("El comentario no puede superar 400 caracteres.");

        var entity = await sad.Solicitudes89.FirstOrDefaultAsync(
            row => row.CodSolicitud == code, cancellationToken);
        if (entity is null) throw new KeyNotFoundException("La solicitud no existe.");
        entity.ComentarioValidacion = comment.Length == 0 ? null : comment;
        await sad.SaveChangesAsync(cancellationToken);
        return responses.Success(true);
    }
}
