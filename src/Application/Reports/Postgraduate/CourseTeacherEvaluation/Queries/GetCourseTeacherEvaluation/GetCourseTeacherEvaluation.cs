using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluation;

public sealed record GetCourseTeacherEvaluation(string? Year, string? Type)
    : IRequest<ApiResponse<CourseTeacherEvaluationReportDTO>>;

public sealed class GetCourseTeacherEvaluationHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetCourseTeacherEvaluation, ApiResponse<CourseTeacherEvaluationReportDTO>>
{
    public async Task<ApiResponse<CourseTeacherEvaluationReportDTO>> Handle(
        GetCourseTeacherEvaluation request, CancellationToken ct)
    {
        var year = Text(request.Year);
        if (year.Length != 4 || !year.All(char.IsAsciiDigit))
            throw new ArgumentException("El año debe tener cuatro dígitos.");
        var type = Text(request.Type).ToUpperInvariant();
        if (type is not ("EX" or "FT"))
            throw new ArgumentException("El tipo debe ser EX o FT.");
        if (type == "EX")
        {
            var rows = (await procedures.SpRec_EvaPostgradoCursosDocentesAsync(year, "EX", cancellationToken: ct) ?? [])
                .Select(row => new ExecutiveCourseEvaluationDTO(
                    Text(row.Periodo), Text(row.Cod_Catedra), Text(row.Cod_Seccion), Text(row.Cod_Profesor),
                    Text(row.Nombre), Text(row.Nom_Catedra), Text(row.ModalidadPostgrado), Text(row.Tipo_AlumnoPostgrado),
                    Number(row.Inscritos), Number(row.ContestadasCurso), Text(row.PorcentajeCurso),
                    Number(row.ContestadasDocente), Text(row.PorcentajeDocente), Number(row.C1_1), Number(row.C1_2),
                    Number(row.C1_3), Number(row.C1_4), Number(row.P1_1), Number(row.P1_2), Number(row.P1_3),
                    Number(row.P1_4), Number(row.P1_5), Number(row.P1_6), Number(row.P1_7), Number(row.P1_8),
                    Number(row.P1_9), Number(row.P1_10), Number(row.P1_11), Number(row.P1_12), Number(row.P1_13),
                    Number(row.Nota))).ToArray();
            return responses.Success(new CourseTeacherEvaluationReportDTO("EX", rows, [], rows.Length));
        }
        var fullTime = (await procedures.SpRec_EvaPostgradoCursosDocentesFullTimeAsync(year, "FT", cancellationToken: ct) ?? [])
            .Select(row => new FullTimeCourseEvaluationDTO(
                Text(row.Periodo), Text(row.Cod_Catedra), Text(row.Cod_Seccion), Text(row.Cod_Profesor),
                Text(row.Nombre), Text(row.Nom_Catedra), Text(row.ModalidadPostgrado), Text(row.Tipo_AlumnoPostgrado),
                Number(row.Inscritos), Number(row.ContestadasCurso), Text(row.PorcentajeCurso),
                Number(row.ContestadasDocente), Text(row.PorcentajeDocente), Number(row.C1_1), Number(row.C1_2),
                Number(row.C1_3), Number(row.C1_4), Number(row.C1_5), Number(row.C1_6), Number(row.C1_7),
                Number(row.C1_8), Number(row.C1_9), Number(row.C1_10), Number(row.C1_11), Number(row.C1_12),
                Number(row.C1_13), Number(row.C1_14), Number(row.C1_15), Number(row.C1_16), Number(row.C1_17),
                Number(row.C1_18), Number(row.C1_19), Number(row.C1_20), Number(row.C1_21), Number(row.C1_22),
                Number(row.C1_23), Number(row.P1_1), Number(row.P1_2), Number(row.P1_3), Number(row.P1_4),
                Number(row.P1_5), Number(row.P1_6), Number(row.P1_7), Number(row.P1_8), Number(row.P1_9),
                Number(row.P1_10), Number(row.P1_11), Number(row.P1_12), Number(row.P1_13), Number(row.Nota)))
            .ToArray();
        return responses.Success(new CourseTeacherEvaluationReportDTO("FT", [], fullTime, fullTime.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Number(double? value) => value?.ToString("0.00", CultureInfo.InvariantCulture) ?? "";
}
