using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityEmailStatus;

public sealed record GetTeacherAvailabilityEmailStatus(Guid JobId)
    : IRequest<ApiResponse<TeacherAvailabilityEmailSnapshot>>;

public sealed class GetTeacherAvailabilityEmailStatusHandler(
    TeacherAvailabilityEmailRegistry registry, IApiResponseService responses)
    : IRequestHandler<GetTeacherAvailabilityEmailStatus, ApiResponse<TeacherAvailabilityEmailSnapshot>>
{
    public Task<ApiResponse<TeacherAvailabilityEmailSnapshot>> Handle(
        GetTeacherAvailabilityEmailStatus request, CancellationToken cancellationToken) =>
        Task.FromResult(responses.Success(
            registry.Snapshot(request.JobId)
            ?? throw new KeyNotFoundException("El envío de correo no existe.")));
}
