using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailPreview;

public sealed record GetStudentsWithoutPhotoEmailPreview
    : IRequest<ApiResponse<EmailPreviewDTO>>;

public sealed class GetStudentsWithoutPhotoEmailPreviewHandler(IApiResponseService responses)
    : IRequestHandler<GetStudentsWithoutPhotoEmailPreview, ApiResponse<EmailPreviewDTO>>
{
    public Task<ApiResponse<EmailPreviewDTO>> Handle(
        GetStudentsWithoutPhotoEmailPreview request, CancellationToken cancellationToken) =>
        Task.FromResult(responses.Success(new EmailPreviewDTO(
            StudentsWithoutPhotoEmailTemplate.Subject, StudentsWithoutPhotoEmailTemplate.Body)));
}
