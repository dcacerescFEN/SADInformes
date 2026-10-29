using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollment;

public sealed record GetExitSurveyEnrollment(string? StudentType, string? Modality, string? Year, bool CanEnroll = true)
    : IRequest<ApiResponse<ExitSurveyEnrollmentDTO>>;
public sealed class GetExitSurveyEnrollmentHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyEnrollment, ApiResponse<ExitSurveyEnrollmentDTO>>
{
    public async Task<ApiResponse<ExitSurveyEnrollmentDTO>> Handle(GetExitSurveyEnrollment request, CancellationToken ct)
    {
        var rows = await ExitSurveyRules.ReadListAsync(procedures, request.StudentType, request.Modality, request.Year, ct);
        var dto = rows.Select(row => new ExitSurveyStudentDTO(row.StudentType, row.Modality, row.PersonCode, row.Name,
            row.StudentCode, row.EntrySemester, ExitSurveyRules.Date(row.ExitDate), ExitSurveyRules.Date(row.DegreeDate),
            row.Enrolled, row.Progress)).ToArray();
        return responses.Success(new ExitSurveyEnrollmentDTO(dto, dto.Length, request.CanEnroll));
    }
}
