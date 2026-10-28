namespace Application.Reports.Postgraduate.MassEnrollment.Common;

internal static class MassEnrollmentRules
{
    internal static async Task<bool> ValidateAvailableAsync(IMassEnrollmentData data, EnrollmentSelection selection, string user,
        CancellationToken ct, bool requireVacancies = true)
    {
        ValidateIdentity(user);
        Validate(selection, required: true);
        var filters = await data.GetFiltersAsync(selection, user, ct);
        if (!filters.StudentTypes.Any(item => item.Value == selection.StudentType)
            || !filters.Modalities.Contains(selection.Modality)
            || !filters.Cohorts.Contains(selection.Cohort)
            || !filters.Periods.Any(item => item.Value == selection.Period))
            throw new ArgumentException("Los filtros seleccionados no están disponibles.");
        if (filters.Courses.Any(item => item.Value == selection.Course)) return true;

        // Absence from the available catalogue is not proof that a course exists.
        // Only an authoritative offered-course lookup permits a read-only listing.
        var course = await data.FindCourseAsync(selection, ct);
        if (course is null || course.Value != selection.Course)
            throw new ArgumentException("Los filtros seleccionados no están disponibles.");
        if (requireVacancies)
            throw new EnrollmentConflictException(MassEnrollmentReport.NoVacanciesMessage);
        return false;
    }

    internal static void ValidateIdentity(string user)
    {
        // The catalog SP accepts varchar(20); never silently truncate an audit identity.
        if (string.IsNullOrWhiteSpace(user) || user.Length > 20)
            throw new ArgumentException("No se pudo identificar al usuario para la inscripción.");
    }

    internal static void Validate(EnrollmentSelection selection, bool required)
    {
        foreach (var (value, limit) in new[] {
            (selection.StudentType, 30), (selection.Modality, 30), (selection.Cohort, 4),
            (selection.Period, 5), (selection.Course, 30) })
        {
            if (string.IsNullOrEmpty(value) && !required) continue;
            if (string.IsNullOrWhiteSpace(value) || value.Length > limit || value != value.Trim()
                || value.Equals("Todos", StringComparison.OrdinalIgnoreCase)
                || value is "*" or "%" || value.Contains('%'))
                throw new ArgumentException("Seleccione tipo, modalidad, cohorte, período y curso concretos.");
        }
        if (!string.IsNullOrEmpty(selection.Course))
        {
            var parts = selection.Course.Split('/');
            if (parts.Length != 3 || parts[0].Length is < 1 or > 30 || parts[1].Length is < 1 or > 2
                || parts[2].Length is < 1 or > 2)
                throw new ArgumentException("El curso seleccionado no es válido.");
        }
    }
}
