namespace Application.Reports.Postgraduate.TeacherSurvey;

/// <summary>Opción de filtro con valor para el SP y texto para la interfaz.</summary>
public sealed record TeacherSurveyOption(string Value, string Text);

/// <summary>Fila del informe: evaluación general del curso (A) y del profesor (B).</summary>
public sealed record TeacherSurveyRow(
    string Period, string Course, string Name, string TeacherId, string TeacherName, string Assistants,
    double? A1, double? A2, double? A3, double? A4, string? AComments,
    int? B1, int? B2, int? B3, int? B4, int? B5, int? B6, int? B7,
    int? B8, int? B9, int? B10, int? B11, int? B12, int? B13,
    double? BGrade, string? BComments);

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
