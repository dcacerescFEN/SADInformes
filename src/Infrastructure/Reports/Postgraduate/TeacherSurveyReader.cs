using Application.Common.Interfaces;
using Application.Reports.Postgraduate.TeacherSurvey;

namespace Infrastructure.Reports.Postgraduate;

/// <summary>
/// Traduce los resultados de los procedimientos del SAD al contrato del informe,
/// sin duplicar su lógica de negocio ni abrir conexiones propias.
/// </summary>
public sealed class TeacherSurveyReader(ISadContextProcedures procedures) : ITeacherSurveyReader
{
    public async Task<TeacherSurveyFilters> GetFiltersAsync(string? studentType, CancellationToken cancellationToken)
    {
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: cancellationToken) ?? [];
        var types = await procedures.SpRec_TiposAlumnoAsync(
            "NO", string.Empty, cancellationToken: cancellationToken) ?? [];
        var modalities = await procedures.SpRec_TiposAlumnosModalidadesAsync(
            string.IsNullOrWhiteSpace(studentType) ? "%" : studentType, cancellationToken: cancellationToken) ?? [];

        return new TeacherSurveyFilters(
            periods.Where(row => !string.IsNullOrWhiteSpace(row.Periodo))
                .Select(row => new TeacherSurveyOption(row.Periodo, row.PeriodoLargo?.Trim() ?? row.Periodo))
                .ToArray(),
            types.Where(row => !string.IsNullOrWhiteSpace(row.Tipo))
                .Select(row => new TeacherSurveyOption(row.Tipo, row.Nombre?.Trim() ?? row.Tipo))
                .ToArray(),
            modalities.Select(row => row.Modalidad?.Trim()).Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray());
    }

    public async Task<IReadOnlyList<TeacherSurveyRow>> GetRowsAsync(
        string period, string studentType, string modality, CancellationToken cancellationToken)
    {
        var rows = await procedures.SpRec_Encuesta_DocenteXCursoAsync(
            period, studentType, modality, cancellationToken: cancellationToken) ?? [];
        return rows.Select(row => new TeacherSurveyRow(
            Period: row.Periodo?.Trim() ?? string.Empty,
            Course: row.Curso?.Trim() ?? string.Empty,
            Name: row.Catedra?.Trim() ?? string.Empty,
            TeacherId: row.Cod_Profesor?.Trim() ?? string.Empty,
            TeacherName: row.Nombre_Profesor?.Trim() ?? string.Empty,
            Assistants: row.Ayudantes?.Trim() ?? string.Empty,
            A1: row.A1, A2: row.A2, A3: row.A3, A4: row.A4, AComments: row.AObservaciones,
            B1: row.B1, B2: row.B2, B3: row.B3, B4: row.B4, B5: row.B5, B6: row.B6, B7: row.B7,
            B8: row.B8, B9: row.B9, B10: row.B10, B11: row.B11, B12: row.B12, B13: row.B13,
            BGrade: row.BNota, BComments: row.BObservaciones)).ToArray();
    }
}
