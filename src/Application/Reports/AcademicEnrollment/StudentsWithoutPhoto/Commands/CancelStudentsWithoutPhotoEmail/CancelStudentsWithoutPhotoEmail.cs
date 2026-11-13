using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Commands.CancelStudentsWithoutPhotoEmail;

public sealed record CancelStudentsWithoutPhotoEmail(Guid JobId)
    : IRequest<ApiResponse<EmailDispatchSnapshot>>;

public sealed class CancelStudentsWithoutPhotoEmailHandler(
    EmailDispatchRegistry registry, IApiResponseService responses)
    : IRequestHandler<CancelStudentsWithoutPhotoEmail, ApiResponse<EmailDispatchSnapshot>>
{
    public Task<ApiResponse<EmailDispatchSnapshot>> Handle(
        CancelStudentsWithoutPhotoEmail request, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(request.JobId, out _))
            throw new KeyNotFoundException("El envío de correo no existe.");
        if (!registry.Cancel(request.JobId))
            throw new EmailDispatchConflictException("El envío de correo ya terminó.");
        return Task.FromResult(responses.Success(registry.Snapshot(request.JobId)!));
    }
}
