using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Settings;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.StartTeacherAvailabilityEmail;

public sealed record StartTeacherAvailabilityEmail(string? Period, string? User, string Mode, string? TeacherCode)
    : IRequest<ApiResponse<TeacherAvailabilityEmailStartedDTO>>;

public sealed class StartTeacherAvailabilityEmailHandler(
    TeacherAvailabilityEmailRegistry registry, TeacherAvailabilityEmailQueue queue,
    IApiResponseService responses)
    : IRequestHandler<StartTeacherAvailabilityEmail, ApiResponse<TeacherAvailabilityEmailStartedDTO>>
{
    public Task<ApiResponse<TeacherAvailabilityEmailStartedDTO>> Handle(
        StartTeacherAvailabilityEmail request, CancellationToken cancellationToken)
    {
        if (!EmailDeliverySettings.IsEnabled)
            throw new InvalidOperationException("El envío de correo está desactivado.");
        if (!EmailDeliverySettings.IsSmtpConfigured)
            throw new InvalidOperationException("La configuración SMTP no está disponible.");
        if (registry.HasActiveJob)
            throw new TeacherAvailabilityEmailConflictException("Ya hay un envío de correo en curso.");

        var mode = request.Mode.Trim().ToLowerInvariant();
        if (mode is not ("single" or "all" or "welcome-pending" or "pending"))
            throw new ArgumentException("El modo de envío no es válido.");
        var job = registry.Create(new TeacherAvailabilityEmailJob
        {
            Period = request.Period, User = request.User, Mode = mode, TeacherCode = request.TeacherCode,
        });
        if (!queue.TryEnqueue(job.Id))
        {
            registry.Fail(job.Id);
            throw new InvalidOperationException("No se pudo iniciar el envío de correo.");
        }
        return Task.FromResult(responses.Success(new TeacherAvailabilityEmailStartedDTO(
            job.Id, TeacherAvailabilityEmailStatus.Queued.ToString().ToUpperInvariant())));
    }
}
