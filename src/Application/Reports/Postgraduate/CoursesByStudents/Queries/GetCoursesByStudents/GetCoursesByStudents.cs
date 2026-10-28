using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudents;

public sealed record GetCoursesByStudents(string Period, string StudentType, string Modality)
    : IRequest<ApiResponse<CoursesByStudentsReportDTO>>;

public sealed class GetCoursesByStudentsHandler(ICoursesByStudentsReader reader, IApiResponseService responses)
    : IRequestHandler<GetCoursesByStudents, ApiResponse<CoursesByStudentsReportDTO>>
{
    public async Task<ApiResponse<CoursesByStudentsReportDTO>> Handle(
        GetCoursesByStudents request, CancellationToken cancellationToken)
    {
        var period = ConcreteFilter.Required("El período es obligatorio.", request.Period);
        var studentType = ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        var modality = ConcreteFilter.Required("Seleccione una modalidad.", request.Modality);

        var rows = await reader.GetRowsAsync(period, studentType, modality, cancellationToken);
        return responses.Success(new CoursesByStudentsReportDTO(rows, rows.Count));
    }
}
