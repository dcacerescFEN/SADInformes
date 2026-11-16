using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.CancelTeacherAvailabilityEmail;

public sealed record CancelTeacherAvailabilityEmail(Guid JobId)
    : IRequest<ApiResponse<TeacherAvailabilityEmailSnapshot>>;

public sealed class CancelTeacherAvailabilityEmailHandler(
    TeacherAvailabilityEmailRegistry registry, IApiResponseService responses)
    : IRequestHandler<CancelTeacherAvailabilityEmail, ApiResponse<TeacherAvailabilityEmailSnapshot>>
{
    public Task<ApiResponse<TeacherAvailabilityEmailSnapshot>> Handle(
        CancelTeacherAvailabilityEmail request, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(request.JobId, out _))
            throw new KeyNotFoundException("El envío de correo no existe.");
        if (!registry.Cancel(request.JobId))
            throw new TeacherAvailabilityEmailConflictException("El envío de correo ya terminó.");
        return Task.FromResult(responses.Success(registry.Snapshot(request.JobId)!));
    }
}
