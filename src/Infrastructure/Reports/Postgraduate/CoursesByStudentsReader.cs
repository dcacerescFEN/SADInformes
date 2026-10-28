using Application.Common.Interfaces;
using Application.Reports.Postgraduate.CoursesByStudents;
using Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudents;
using Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudentsFilters;

namespace Infrastructure.Reports.Postgraduate;

/// <summary>
/// Traduce los resultados de los procedimientos del SAD al contrato del informe,
/// sin duplicar su lógica de negocio ni abrir conexiones propias.
/// </summary>
public sealed class CoursesByStudentsReader(ISadContextProcedures procedures) : ICoursesByStudentsReader
{
    public async Task<CoursesByStudentsFiltersDTO> GetFiltersAsync(string? studentType, CancellationToken cancellationToken)
    {
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: cancellationToken) ?? [];
        var types = await procedures.SpRec_TiposAlumnoAsync(
            "NO", string.Empty, cancellationToken: cancellationToken) ?? [];
        var modalities = string.IsNullOrWhiteSpace(studentType)
            ? []
            : await procedures.SpRec_TiposAlumnosModalidadesAsync(studentType, cancellationToken: cancellationToken) ?? [];

        return new CoursesByStudentsFiltersDTO(
            periods.Where(row => !string.IsNullOrWhiteSpace(row.Periodo))
                .Select(row => new CoursesByStudentsOptionDTO(row.Periodo, row.PeriodoLargo?.Trim() ?? row.Periodo))
                .ToArray(),
            types.Where(row => !string.IsNullOrWhiteSpace(row.Tipo))
                .Select(row => new CoursesByStudentsOptionDTO(row.Tipo, row.Nombre?.Trim() ?? row.Tipo))
                .ToArray(),
            modalities.Select(row => row.Modalidad?.Trim()).Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray());
    }

    public async Task<IReadOnlyList<CoursesByStudentsDTO>> GetRowsAsync(
        string period, string studentType, string modality, CancellationToken cancellationToken)
    {
        var rows = await procedures.SpRec_AlumnosXPeriodoXTipo_AlumnoXModalidadAsync(
            period, studentType, modality, cancellationToken: cancellationToken) ?? [];
        return rows.Select(row => new CoursesByStudentsDTO(
            Period: row.Periodo?.Trim() ?? string.Empty,
            Course: row.Curso?.Trim() ?? string.Empty,
            Name: row.Catedra?.Trim() ?? string.Empty,
            StudentCode: row.Cod_Alumno?.Trim() ?? string.Empty,
            StudentName: row.Nombre?.Trim() ?? string.Empty,
            EntrySemester: row.SemestreIngreso?.Trim() ?? string.Empty)).ToArray();
    }
}
