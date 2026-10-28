using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.UnansweredTeacherSurvey.Queries.GetUnansweredTeacherSurvey;

public sealed record GetUnansweredTeacherSurvey(string Period, string StudentType, string Modality)
    : IRequest<ApiResponse<UnansweredTeacherSurveyReportDTO>>;

public sealed class GetUnansweredTeacherSurveyHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetUnansweredTeacherSurvey, ApiResponse<UnansweredTeacherSurveyReportDTO>>
{
    public async Task<ApiResponse<UnansweredTeacherSurveyReportDTO>> Handle(
        GetUnansweredTeacherSurvey request, CancellationToken cancellationToken)
    {
        var period = ConcreteFilter.Required("El período es obligatorio.", request.Period);
        var studentType = ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        var modality = ConcreteFilter.Required("Seleccione una modalidad.", request.Modality);

        if (period.Length > 20 || studentType.Length > 30 || modality.Length > 30)
            throw new ArgumentException("Los filtros exceden la longitud permitida.");

        var rows = await procedures.SpRec_Alumnos_Sin_Evaluacion_DocenteAsync(
            period, studentType, modality, cancellationToken: cancellationToken) ?? [];
        var mappedRows = rows.Select(row => new UnansweredTeacherSurveyDTO(
            Period: row.Periodo?.Trim() ?? string.Empty,
            Course: row.Cod_Catedra.Trim() + "/" + row.Cod_Seccion.Trim(),
            Name: row.Catedra?.Trim() ?? string.Empty,
            StudentCode: row.Cod_Alumno?.Trim() ?? string.Empty,
            StudentName: row.Nombre?.Trim() ?? string.Empty)).ToArray();
        return responses.Success(new UnansweredTeacherSurveyReportDTO(mappedRows, mappedRows.Length));
    }
}
