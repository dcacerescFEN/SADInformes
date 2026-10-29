using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsTeachers;

public sealed record GetExitSurveyStatisticsTeachers(string? Period, string? StudentType, string? Modality)
    : IRequest<ApiResponse<ExitSurveyTeachersDTO>>;
public sealed class GetExitSurveyStatisticsTeachersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsTeachers, ApiResponse<ExitSurveyTeachersDTO>>
{
    public async Task<ApiResponse<ExitSurveyTeachersDTO>> Handle(GetExitSurveyStatisticsTeachers request, CancellationToken ct)
    {
        var period = ExitSurveyStatisticsRules.Text(request.Period, "El período no es válido.", 5);
        var type = ExitSurveyStatisticsRules.Type(ExitSurveyStatisticsRules.Text(request.StudentType, "El tipo de alumno no es válido.", 30));
        var modality = ExitSurveyStatisticsRules.Text(request.Modality, "La modalidad no es válida.", 10);
        var theoretical = await procedures.spRec_EncuestaGraduadosEstadisticasProfesoresIAsync(period, type, modality, cancellationToken: ct) ?? [];
        var practical = await procedures.spRec_EncuestaGraduadosEstadisticasProfesoresIIAsync(period, type, modality, cancellationToken: ct) ?? [];
        var formative = await procedures.spRec_EncuestaGraduadosEstadisticasProfesoresIIIAsync(period, type, modality, cancellationToken: ct) ?? [];
        return responses.Success(new ExitSurveyTeachersDTO(Map(theoretical), Map(practical), Map(formative)));
    }

    private static ExitSurveyTeacherDTO[] Map(IEnumerable<Domain.Entities.Sad.Sp.SpRecEncuestaGraduadosProfesoresResult> rows) =>
        rows.Select(row => new ExitSurveyTeacherDTO(row.Profesor?.Trim() ?? "", row.Frecuencia ?? 0)).ToArray();
}
