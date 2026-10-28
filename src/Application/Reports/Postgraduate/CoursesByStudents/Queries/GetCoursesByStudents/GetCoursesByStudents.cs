using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudents;

public sealed record GetCoursesByStudents(string Period, string StudentType, string Modality)
    : IRequest<ApiResponse<CoursesByStudentsReportDTO>>;

public sealed class GetCoursesByStudentsHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetCoursesByStudents, ApiResponse<CoursesByStudentsReportDTO>>
{
    public async Task<ApiResponse<CoursesByStudentsReportDTO>> Handle(
        GetCoursesByStudents request, CancellationToken cancellationToken)
    {
        var period = ConcreteFilter.Required("El período es obligatorio.", request.Period);
        var studentType = ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        var modality = ConcreteFilter.Required("Seleccione una modalidad.", request.Modality);

        var rows = await procedures.SpRec_AlumnosXPeriodoXTipo_AlumnoXModalidadAsync(
            period, studentType, modality, cancellationToken: cancellationToken) ?? [];
        var mappedRows = rows.Select(row => new CoursesByStudentsDTO(
            Period: row.Periodo?.Trim() ?? string.Empty,
            Course: row.Curso?.Trim() ?? string.Empty,
            Name: row.Catedra?.Trim() ?? string.Empty,
            StudentCode: row.Cod_Alumno?.Trim() ?? string.Empty,
            StudentName: row.Nombre?.Trim() ?? string.Empty,
            EntrySemester: row.SemestreIngreso?.Trim() ?? string.Empty)).ToArray();
        return responses.Success(new CoursesByStudentsReportDTO(mappedRows, mappedRows.Length));
    }
}
