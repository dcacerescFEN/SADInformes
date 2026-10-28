using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Validation;
using Domain.Common;

namespace Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudentsFilters;

public sealed record GetCoursesByStudentsFilters(string? StudentType)
    : IRequest<ApiResponse<CoursesByStudentsFiltersDTO>>;

public sealed class GetCoursesByStudentsFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetCoursesByStudentsFilters, ApiResponse<CoursesByStudentsFiltersDTO>>
{
    public async Task<ApiResponse<CoursesByStudentsFiltersDTO>> Handle(
        GetCoursesByStudentsFilters request, CancellationToken cancellationToken)
    {
        var studentType = string.IsNullOrWhiteSpace(request.StudentType) ? null
            : ConcreteFilter.Required("Seleccione un tipo de alumno.", request.StudentType);
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: cancellationToken) ?? [];
        var types = await procedures.SpRec_TiposAlumnoAsync(
            "NO", string.Empty, cancellationToken: cancellationToken) ?? [];
        var modalities = string.IsNullOrWhiteSpace(studentType)
            ? []
            : await procedures.SpRec_TiposAlumnosModalidadesAsync(studentType, cancellationToken: cancellationToken) ?? [];

        var filters = new CoursesByStudentsFiltersDTO(
            periods.Where(row => !string.IsNullOrWhiteSpace(row.Periodo))
                .Select(row => new CoursesByStudentsOptionDTO(row.Periodo, row.PeriodoLargo?.Trim() ?? row.Periodo))
                .ToArray(),
            types.Where(row => !string.IsNullOrWhiteSpace(row.Tipo))
                .Select(row => new CoursesByStudentsOptionDTO(row.Tipo, row.Nombre?.Trim() ?? row.Tipo))
                .ToArray(),
            modalities.Select(row => row.Modalidad?.Trim()).Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray());
        return responses.Success(filters);
    }
}
