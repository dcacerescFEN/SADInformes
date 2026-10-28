using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurveyFilters;

public sealed record GetTeacherSurveyFilters(string? StudentType)
    : IRequest<ApiResponse<TeacherSurveyFiltersDTO>>;

public sealed class GetTeacherSurveyFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherSurveyFilters, ApiResponse<TeacherSurveyFiltersDTO>>
{
    public async Task<ApiResponse<TeacherSurveyFiltersDTO>> Handle(
        GetTeacherSurveyFilters request, CancellationToken cancellationToken)
    {
        var studentType = request.StudentType?.Trim();
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: cancellationToken) ?? [];
        var types = await procedures.SpRec_TiposAlumnoAsync(
            "NO", string.Empty, cancellationToken: cancellationToken) ?? [];
        var modalities = await procedures.SpRec_TiposAlumnosModalidadesAsync(
            string.IsNullOrWhiteSpace(studentType) ? "%" : studentType, cancellationToken: cancellationToken) ?? [];

        var filters = new TeacherSurveyFiltersDTO(
            periods.Where(row => !string.IsNullOrWhiteSpace(row.Periodo))
                .Select(row => new TeacherSurveyOptionDTO(row.Periodo, row.PeriodoLargo?.Trim() ?? row.Periodo))
                .ToArray(),
            types.Where(row => !string.IsNullOrWhiteSpace(row.Tipo))
                .Select(row => new TeacherSurveyOptionDTO(row.Tipo, row.Nombre?.Trim() ?? row.Tipo))
                .ToArray(),
            modalities.Select(row => row.Modalidad?.Trim()).Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray());
        return responses.Success(filters);
    }
}
