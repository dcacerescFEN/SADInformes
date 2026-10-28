using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.UnansweredTeacherSurvey.Queries.GetUnansweredTeacherSurveyFilters;

public sealed record GetUnansweredTeacherSurveyFilters(string? StudentType)
    : IRequest<ApiResponse<UnansweredTeacherSurveyFiltersDTO>>;

public sealed class GetUnansweredTeacherSurveyFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetUnansweredTeacherSurveyFilters, ApiResponse<UnansweredTeacherSurveyFiltersDTO>>
{
    public async Task<ApiResponse<UnansweredTeacherSurveyFiltersDTO>> Handle(
        GetUnansweredTeacherSurveyFilters request, CancellationToken cancellationToken)
    {
        var studentType = string.IsNullOrWhiteSpace(request.StudentType) ? null
            : ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        if (studentType?.Length > 30)
            throw new ArgumentException("El tipo de alumno excede la longitud permitida.");
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: cancellationToken) ?? [];
        var types = await procedures.SpRec_TiposAlumnoAsync(
            "NO", string.Empty, cancellationToken: cancellationToken) ?? [];
        var modalities = string.IsNullOrWhiteSpace(studentType)
            ? []
            : await procedures.SpRec_TiposAlumnosModalidadesAsync(studentType, cancellationToken: cancellationToken) ?? [];

        var filters = new UnansweredTeacherSurveyFiltersDTO(
            periods.Where(row => Concrete(row.Periodo))
                .Select(row => new UnansweredTeacherSurveyOptionDTO(row.Periodo!.Trim(), row.PeriodoLargo?.Trim() ?? row.Periodo.Trim()))
                .ToArray(),
            types.Where(row => Concrete(row.Tipo))
                .Select(row => new UnansweredTeacherSurveyOptionDTO(row.Tipo.Trim(), row.Nombre?.Trim() ?? row.Tipo.Trim()))
                .ToArray(),
            modalities.Select(row => row.Modalidad?.Trim()).Where(Concrete)
                .Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray());
        return responses.Success(filters);
    }

    private static bool Concrete(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Trim().Equals("Todos", StringComparison.OrdinalIgnoreCase)
        && value.Trim() != "*" && !value.Contains('%') && !value.Contains('_');
}
