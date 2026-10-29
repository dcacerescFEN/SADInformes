using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using System.Globalization;

namespace Application.Reports.Postgraduate.TeacherCourses.Queries.GetTeacherCourses;

public sealed record GetTeacherCourses(string Year, string Department)
    : IRequest<ApiResponse<TeacherCoursesReportDTO>>;

public sealed class GetTeacherCoursesHandler(
    ISadContextProcedures procedures, IPostgraduateSettings settings, IApiResponseService responses)
    : IRequestHandler<GetTeacherCourses, ApiResponse<TeacherCoursesReportDTO>>
{
    public async Task<ApiResponse<TeacherCoursesReportDTO>> Handle(
        GetTeacherCourses request, CancellationToken cancellationToken)
    {
        TeacherCoursesValidation.Year(request.Year);
        TeacherCoursesValidation.Department(request.Department);

        var years = await procedures.SpRec_PeriodosAcademicosXTipoAsync(
            "AÑO", "POSTGRADO", "19000", cancellationToken: cancellationToken) ?? [];
        if (!years.Any(row => row.Periodo?.Trim() == request.Year))
            throw new ArgumentException("Seleccione un período válido.");

        // spRec_EvaPostgradoCursosDocentes2 does not authorize the department itself:
        // the report is only reachable for departments visible to the configured SAD user.
        var departments = await procedures.SpRec_DocentesDepartamentosAsync(
            "NO", settings.User, cancellationToken: cancellationToken) ?? [];
        var department = departments
            .Select(row => row.Departamento?.Trim())
            .FirstOrDefault(value => value is not null && value.Equals(request.Department.Trim(), StringComparison.OrdinalIgnoreCase));
        if (department is null)
            throw new ArgumentException("El departamento seleccionado no está disponible.");

        var rows = await procedures.SpRec_EvaPostgradoCursosDocentes2Async(
            request.Year, department, cancellationToken: cancellationToken) ?? [];
        var mappedRows = rows.Select(row => new TeacherCoursesDTO(
            ProfessorCode: row.Cod_Profesor?.Trim() ?? string.Empty,
            Name: row.Nombre?.Trim() ?? string.Empty,
            EvaluatedCourses: row.Catedras,
            Average: DecimalText(row.Promedio),
            Classification: row.AQPQ?.Trim() ?? string.Empty)).ToArray();
        return responses.Success(new TeacherCoursesReportDTO(mappedRows, mappedRows.Length));
    }

    /// <summary>
    /// Keeps the decimal(19,6) value exactly as returned by the procedure, with the comma
    /// decimal separator used by the legacy report, and deliberately does not round it.
    /// </summary>
    private static string DecimalText(decimal? value) =>
        value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture).Replace('.', ',');
}
