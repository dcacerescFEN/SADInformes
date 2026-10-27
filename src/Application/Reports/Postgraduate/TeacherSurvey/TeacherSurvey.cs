namespace Application.Reports.Postgraduate.TeacherSurvey;

/// <summary>Opción de filtro con valor para el SP y texto para la interfaz.</summary>
public sealed record TeacherSurveyOption(string Value, string Text);

/// <summary>Fila del informe: evaluación general del curso (A) y del profesor (B).</summary>
public sealed record TeacherSurveyRow(
    string Period, string Course, string Name, string TeacherId, string TeacherName, string Assistants,
    decimal? A1, decimal? A2, decimal? A3, decimal? A4, string? AComments,
    decimal? B1, decimal? B2, decimal? B3, decimal? B4, decimal? B5, decimal? B6, decimal? B7,
    decimal? B8, decimal? B9, decimal? B10, decimal? B11, decimal? B12, decimal? B13,
    decimal? BGrade, string? BComments);

/// <summary>Catálogos que alimentan los selectores del informe.</summary>
public sealed record TeacherSurveyFilters(
    IReadOnlyList<TeacherSurveyOption> Periods,
    IReadOnlyList<TeacherSurveyOption> StudentTypes,
    IReadOnlyList<string> Modalities);

/// <summary>Lectura de solo lectura contra los procedimientos existentes del SAD.</summary>
public interface ITeacherSurveyReader
{
    Task<TeacherSurveyFilters> GetFiltersAsync(string? studentType, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherSurveyRow>> GetRowsAsync(
        string period, string studentType, string modality, CancellationToken cancellationToken);
}
