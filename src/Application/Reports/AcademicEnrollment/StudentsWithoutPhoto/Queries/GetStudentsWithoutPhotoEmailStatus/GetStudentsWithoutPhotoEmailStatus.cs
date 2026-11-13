using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailStatus;

public sealed record GetStudentsWithoutPhotoEmailStatus(Guid JobId)
    : IRequest<ApiResponse<EmailDispatchSnapshot>>;

public sealed class GetStudentsWithoutPhotoEmailStatusHandler(
    EmailDispatchRegistry registry, IApiResponseService responses)
    : IRequestHandler<GetStudentsWithoutPhotoEmailStatus, ApiResponse<EmailDispatchSnapshot>>
{
    public Task<ApiResponse<EmailDispatchSnapshot>> Handle(
        GetStudentsWithoutPhotoEmailStatus request, CancellationToken cancellationToken) =>
        Task.FromResult(responses.Success(
            registry.Snapshot(request.JobId)
            ?? throw new KeyNotFoundException("El envío de correo no existe.")));
}
