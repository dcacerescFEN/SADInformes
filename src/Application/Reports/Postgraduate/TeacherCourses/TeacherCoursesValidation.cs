namespace Application.Reports.Postgraduate.TeacherCourses;

internal static class TeacherCoursesValidation
{
    internal static void Year(string? value)
    {
        if (value is null || value.Length != 4 || !value.All(char.IsAsciiDigit))
            throw new ArgumentException("Seleccione un período válido.");
    }

    internal static void Department(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 70)
            throw new ArgumentException("Seleccione un departamento válido.");
    }
}
