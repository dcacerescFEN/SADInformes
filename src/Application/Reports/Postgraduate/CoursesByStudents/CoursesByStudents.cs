namespace Application.Reports.Postgraduate.CoursesByStudents;

/// <summary>Opción de filtro con valor para el SP y texto para la interfaz.</summary>
public sealed record CoursesByStudentsOption(string Value, string Text);

/// <summary>Curso cursado por un alumno del semestre de ingreso seleccionado.</summary>
public sealed record CoursesByStudentsRow(
    string Period, string Course, string Name, string StudentCode, string StudentName, string EntrySemester);

/// <summary>Catálogos que alimentan los selectores del informe.</summary>
public sealed record CoursesByStudentsFilters(
    IReadOnlyList<CoursesByStudentsOption> Periods,
    IReadOnlyList<CoursesByStudentsOption> StudentTypes,
    IReadOnlyList<string> Modalities);

/// <summary>Lectura de solo lectura contra los procedimientos existentes del SAD.</summary>
public interface ICoursesByStudentsReader
{
    Task<CoursesByStudentsFilters> GetFiltersAsync(string? studentType, CancellationToken cancellationToken);

    Task<IReadOnlyList<CoursesByStudentsRow>> GetRowsAsync(
        string period, string studentType, string modality, CancellationToken cancellationToken);
}
