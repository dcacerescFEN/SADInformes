using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyStatistics.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyStatistics.Queries.GetExitSurveyStatisticsTeacherDetail;

public sealed record GetExitSurveyStatisticsTeacherDetail(string? Period, string? StudentType, string? Modality, string? Professor)
    : IRequest<ApiResponse<ExitSurveyTeacherDetailDTO>>;
public sealed class GetExitSurveyStatisticsTeacherDetailHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetExitSurveyStatisticsTeacherDetail, ApiResponse<ExitSurveyTeacherDetailDTO>>
{
    public async Task<ApiResponse<ExitSurveyTeacherDetailDTO>> Handle(GetExitSurveyStatisticsTeacherDetail request, CancellationToken ct)
    {
        var period = ExitSurveyStatisticsRules.Text(request.Period, "El período no es válido.", 5);
        var type = ExitSurveyStatisticsRules.Type(ExitSurveyStatisticsRules.Text(request.StudentType, "El tipo de alumno no es válido.", 30));
        var modality = ExitSurveyStatisticsRules.Text(request.Modality, "La modalidad no es válida.", 10);
        var professor = request.Professor?.Trim();
        if (string.IsNullOrEmpty(professor) || professor.Length > 20 || professor.Any(char.IsControl))
            throw new ArgumentException("El profesor no es válido.");
        var data = await procedures.spRec_EncuestaGraduadosEstadisticasDetalleProfAsync(period, type, modality, professor, cancellationToken: ct) ?? [];
        return responses.Success(new ExitSurveyTeacherDetailDTO(professor, data.Select(row => row.Motivos ?? "").ToArray()));
    }
}
