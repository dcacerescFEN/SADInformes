namespace Application.Reports.Postgraduate.TeachingSchedule;

internal static class TeachingScheduleValidation
{
    internal static void PeriodType(string value)
    {
        if (value is not ("PERÍODO" or "SEMESTRE" or "AÑO"))
            throw new ArgumentException("Seleccione un tipo de período válido.");
    }

    internal static void Text(string? value, int maxLength, bool required = false)
    {
        if ((required && string.IsNullOrWhiteSpace(value)) || value?.Length > maxLength)
            throw new ArgumentException("Seleccione filtros válidos para la programación docente.");
    }
}
