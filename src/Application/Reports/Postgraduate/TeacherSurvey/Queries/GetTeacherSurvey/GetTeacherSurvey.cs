using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurvey;

public sealed record GetTeacherSurvey(string Period, string StudentType, string Modality)
    : IRequest<ApiResponse<TeacherSurveyReportDTO>>;

public sealed class GetTeacherSurveyHandler(ITeacherSurveyReader reader, IApiResponseService responses)
    : IRequestHandler<GetTeacherSurvey, ApiResponse<TeacherSurveyReportDTO>>
{
    public async Task<ApiResponse<TeacherSurveyReportDTO>> Handle(
        GetTeacherSurvey request, CancellationToken cancellationToken)
    {
        var period = ConcreteFilter.Required("El período es obligatorio.", request.Period);
        var studentType = ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        var modality = ConcreteFilter.Required("Seleccione una modalidad.", request.Modality);

        var rows = await reader.GetRowsAsync(period, studentType, modality, cancellationToken);
        return responses.Success(new TeacherSurveyReportDTO(rows, rows.Count));
    }
}
