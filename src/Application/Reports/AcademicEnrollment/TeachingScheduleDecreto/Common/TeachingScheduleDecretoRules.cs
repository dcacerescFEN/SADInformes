namespace Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Common;

internal static class TeachingScheduleDecretoRules
{
    public static string Period(string? value)
    {
        var period = value?.Trim();
        return !string.IsNullOrEmpty(period) && period.Length <= 5
            ? period
            : throw new ArgumentException("El período no es válido.");
    }
}
