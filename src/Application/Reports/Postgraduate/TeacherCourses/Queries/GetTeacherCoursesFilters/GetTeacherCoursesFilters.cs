using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherCourses.Queries.GetTeacherCoursesFilters;

public sealed record GetTeacherCoursesFilters : IRequest<ApiResponse<TeacherCoursesFiltersDTO>>;

public sealed class GetTeacherCoursesFiltersHandler(
    ISadContextProcedures procedures, IPostgraduateSettings settings, IApiResponseService responses)
    : IRequestHandler<GetTeacherCoursesFilters, ApiResponse<TeacherCoursesFiltersDTO>>
{
    public async Task<ApiResponse<TeacherCoursesFiltersDTO>> Handle(
        GetTeacherCoursesFilters request, CancellationToken cancellationToken)
    {
        var years = await procedures.SpRec_PeriodosAcademicosXTipoAsync(
            "AÑO", "POSTGRADO", "19000", cancellationToken: cancellationToken) ?? [];
        // The legacy procedure scopes departments to the configured SAD user.
        var departments = await procedures.SpRec_DocentesDepartamentosAsync(
            "NO", settings.User, cancellationToken: cancellationToken) ?? [];
        return responses.Success(new TeacherCoursesFiltersDTO(
            years.Where(row => Concrete(row.Periodo))
                .Select(row => new TeacherCoursesOptionDTO(row.Periodo!.Trim(), row.PeriodoLargo?.Trim() ?? row.Periodo.Trim()))
                .ToArray(),
            departments.Where(row => Concrete(row.Departamento))
                .Select(row => new TeacherCoursesOptionDTO(row.Departamento!.Trim(), row.Departamento.Trim()))
                .ToArray()));
    }

    private static bool Concrete(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Trim().Equals("Todos", StringComparison.OrdinalIgnoreCase)
        && value.Trim() != "*" && !value.Contains('%') && !value.Contains('_');
}
