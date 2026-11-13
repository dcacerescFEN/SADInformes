using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Settings;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Commands.StartStudentsWithoutPhotoEmail;

public sealed record StartStudentsWithoutPhotoEmail
    : IRequest<ApiResponse<EmailDispatchStartedDTO>>;

public sealed class StartStudentsWithoutPhotoEmailHandler(
    EmailDispatchRegistry registry,
    EmailDispatchQueue queue,
    IApiResponseService responses
) : IRequestHandler<StartStudentsWithoutPhotoEmail, ApiResponse<EmailDispatchStartedDTO>>
{
    public Task<ApiResponse<EmailDispatchStartedDTO>> Handle(
        StartStudentsWithoutPhotoEmail request,
        CancellationToken cancellationToken
    )
    {
        if (!EmailDeliverySettings.IsEnabled)
            throw new InvalidOperationException("El envío de correo está desactivado.");
        if (!EmailDeliverySettings.IsSmtpConfigured)
            throw new InvalidOperationException("La configuración SMTP no está disponible.");
        if (registry.HasActiveJob)
            throw new EmailDispatchConflictException("Ya hay un envío de correo en curso.");

        var job = registry.Create();
        if (!queue.TryEnqueue(job.Id))
        {
            registry.Fail(job.Id);
            throw new InvalidOperationException("No se pudo iniciar el envío de correo.");
        }

        return Task.FromResult(
            responses.Success(
                new EmailDispatchStartedDTO(
                    job.Id,
                    EmailDispatchStatus.Queued.ToString().ToUpperInvariant()
                )
            )
        );
    }
}
