using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurvey;

public sealed record GetTeacherSurvey(string Period, string StudentType, string Modality)
    : IRequest<ApiResponse<TeacherSurveyReportDTO>>;

public sealed class GetTeacherSurveyHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherSurvey, ApiResponse<TeacherSurveyReportDTO>>
{
    public async Task<ApiResponse<TeacherSurveyReportDTO>> Handle(
        GetTeacherSurvey request, CancellationToken cancellationToken)
    {
        var period = ConcreteFilter.Required("El período es obligatorio.", request.Period);
        var studentType = ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        var modality = ConcreteFilter.Required("Seleccione una modalidad.", request.Modality);

        var rows = await procedures.SpRec_Encuesta_DocenteXCursoAsync(
            period, studentType, modality, cancellationToken: cancellationToken) ?? [];
        var mappedRows = rows.Select(row => new TeacherSurveyDTO(
            Period: row.Periodo?.Trim() ?? string.Empty,
            Course: row.Curso?.Trim() ?? string.Empty,
            Name: row.Catedra?.Trim() ?? string.Empty,
            TeacherId: row.Cod_Profesor?.Trim() ?? string.Empty,
            TeacherName: row.Nombre_Profesor?.Trim() ?? string.Empty,
            Assistants: row.Ayudantes?.Trim() ?? string.Empty,
            A1: row.A1, A2: row.A2, A3: row.A3, A4: row.A4, AComments: row.AObservaciones,
            B1: row.B1, B2: row.B2, B3: row.B3, B4: row.B4, B5: row.B5, B6: row.B6, B7: row.B7,
            B8: row.B8, B9: row.B9, B10: row.B10, B11: row.B11, B12: row.B12, B13: row.B13,
            BGrade: row.BNota, BComments: row.BObservaciones)).ToArray();
        return responses.Success(new TeacherSurveyReportDTO(mappedRows, mappedRows.Length));
    }
}
