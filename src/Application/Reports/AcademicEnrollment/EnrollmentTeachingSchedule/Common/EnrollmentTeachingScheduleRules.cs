namespace Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Common;

internal static class EnrollmentTeachingScheduleRules
{
    public const string Undergraduate = "PREGRADO";
    public const string Postgraduate = "POSTGRADO";

    public static string School(string? value)
    {
        var school = value?.Trim().ToUpperInvariant();
        return school is Undergraduate or Postgraduate
            ? school
            : throw new ArgumentException("La escuela no es válida.");
    }

    public static string Period(string? value)
    {
        var period = value?.Trim();
        return !string.IsNullOrEmpty(period) && period.Length <= 5
            ? period
            : throw new ArgumentException("El período no es válido.");
    }

    public static string Area(string? value)
    {
        var area = value?.Trim();
        if (string.IsNullOrEmpty(area)) return "%";
        return area.Length <= 100 ? area : throw new ArgumentException("El área no es válida.");
    }

    public static string Coordination(string school) =>
        school == Postgraduate ? "EPOST-TODO" : "%";
}
