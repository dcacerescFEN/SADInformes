using System.Globalization;
using Application.Common.Interfaces;
using Domain.Entities.Sad.Sp;

namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Common;

internal sealed record ExitSurveyStudent(
    string StudentType, string Modality, string PersonCode, string Name, string StudentCode,
    string EntrySemester, DateTime? ExitDate, DateTime? DegreeDate, bool Enrolled, string? Progress);

internal static class ExitSurveyRules
{
    internal static string Text(string? value, string message)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Todos" : value.Trim();
        if (text.Length > 30 || text.Any(char.IsControl)) throw new ArgumentException(message);
        return text;
    }

    internal static string Year(string? value)
    {
        var text = value?.Trim();
        if (text is not { Length: 4 } || !text.All(char.IsAsciiDigit) || text == "0000")
            throw new ArgumentException("El año no es válido.");
        return text;
    }

    internal static string? YearOrNull(string? value) =>
        value is { Length: 4 } && value.All(char.IsAsciiDigit) && value != "0000" ? value : null;

    internal static string PeriodFromCode(string code)
    {
        var index = code.LastIndexOf('-');
        if (index < 0 || index == code.Length - 1) throw new ArgumentException("El código de alumno no contiene un período válido.");
        var period = code[(index + 1)..];
        if (period.Length is < 1 or > 5 || !period.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException("El código de alumno no contiene un período válido.");
        return period;
    }

    internal static async Task<ExitSurveyStudent[]> ReadListAsync(ISadContextProcedures procedures,
        string? type, string? modality, string? year, CancellationToken ct)
    {
        var rows = await procedures.spRec_EncuestaGraduados_AlumnosAsync(
            Text(type, "El tipo de alumno no es válido."), Text(modality, "La modalidad no es válida."),
            Year(year), cancellationToken: ct) ?? [];
        return rows.Where(row => !string.IsNullOrWhiteSpace(row.CodAlumno))
            .DistinctBy(row => row.CodAlumno!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(row => new ExitSurveyStudent(
                row.TipoAlumno?.Trim() ?? "", row.Modalidad?.Trim() ?? "", row.CodPersona?.Trim() ?? "",
                row.Nombre?.Trim() ?? "", row.CodAlumno!.Trim(), row.SemIngreso?.Trim() ?? "",
                row.FechaEgreso, row.FechaTitulacion, string.Equals(row.Inscrito?.Trim(), "SI", StringComparison.OrdinalIgnoreCase),
                string.IsNullOrWhiteSpace(row.Porcentaje) ? null : row.Porcentaje.Trim())).ToArray();
    }

    internal static string Date(DateTime? value) => value?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) ?? "";
    internal static string? SpDate(DateTime? value) => value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
