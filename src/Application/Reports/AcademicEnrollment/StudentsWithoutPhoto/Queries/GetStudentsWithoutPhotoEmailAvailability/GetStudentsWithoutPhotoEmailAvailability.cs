using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Settings;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailAvailability;

public sealed record GetStudentsWithoutPhotoEmailAvailability
    : IRequest<ApiResponse<EmailAvailabilityDTO>>;

public sealed class GetStudentsWithoutPhotoEmailAvailabilityHandler(IApiResponseService responses)
    : IRequestHandler<GetStudentsWithoutPhotoEmailAvailability, ApiResponse<EmailAvailabilityDTO>>
{
    public Task<ApiResponse<EmailAvailabilityDTO>> Handle(
        GetStudentsWithoutPhotoEmailAvailability request, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(responses.Success(new EmailAvailabilityDTO(
                EmailDeliverySettings.IsEnabled, EmailDeliverySettings.IsSmtpConfigured)));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(responses.Success(new EmailAvailabilityDTO(false, false)));
        }
    }
}
