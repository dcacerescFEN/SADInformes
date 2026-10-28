namespace Application.Reports.Postgraduate.MassEnrollment;

public sealed class MassEnrollmentService(IMassEnrollmentData data)
{
    public async Task<MassEnrollmentFilters> FiltersAsync(EnrollmentSelection selection, string user, CancellationToken ct)
    {
        ValidateIdentity(user);
        Validate(selection, required: false);
        return await data.GetFiltersAsync(selection, user, ct);
    }

    public async Task<MassEnrollmentReport> CandidatesAsync(EnrollmentSelection selection, string user, CancellationToken ct)
    {
        await ValidateAvailableAsync(selection, user, ct);
        var rows = await data.GetCandidatesAsync(selection, ct);
        return new(rows, rows.Count);
    }

    public async Task<EnrollmentResult> EnrollAsync(EnrollmentRequest request, string user, string pc, CancellationToken ct)
    {
        ValidateIdentity(user);
        var selection = request.Selection ?? throw new ArgumentException("Seleccione los filtros de inscripción.");
        Validate(selection, required: true);
        if (!request.Confirmed) throw new ArgumentException("Debe confirmar la inscripción.");
        var students = request.StudentCodes;
        if (students is null || students.Length == 0 || students.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 20)
            || students.Distinct(StringComparer.Ordinal).Count() != students.Length)
            throw new ArgumentException("Seleccione alumnos válidos sin duplicados.");

        // Outer transaction contains the SP's nested transactions and every audit write.
        await using var transaction = await data.BeginAsync(ct);
        await ValidateAvailableAsync(selection, user, ct);
        var rows = await data.GetCandidatesAsync(selection, ct);
        foreach (var student in students.Order(StringComparer.Ordinal))
        {
            var eligible = rows.Where(row => row.StudentCode == student).ToArray();
            if (eligible.Length == 0 || eligible.Any(row => row.Enrolled)
                || await data.IsEnrolledAsync(student, selection, ct))
                throw new EnrollmentConflictException("La selección cambió o incluye alumnos ya inscritos. Actualice el listado.");
        }
        foreach (var student in students.Order(StringComparer.Ordinal))
        {
            await data.AddAsync(student, selection, user, pc, ct);
            // The legacy SP has silent no-op paths; an affected-row count is not proof.
            if (!await data.IsEnrolledAsync(student, selection, ct))
                throw new InvalidOperationException("Enrollment postcondition failed.");
        }
        await transaction.CommitAsync(ct);
        return new(students.Length);
    }

    private async Task ValidateAvailableAsync(EnrollmentSelection selection, string user, CancellationToken ct)
    {
        ValidateIdentity(user);
        Validate(selection, required: true);
        var filters = await data.GetFiltersAsync(selection, user, ct);
        if (!filters.StudentTypes.Any(item => item.Value == selection.StudentType)
            || !filters.Modalities.Contains(selection.Modality)
            || !filters.Cohorts.Contains(selection.Cohort)
            || !filters.Periods.Any(item => item.Value == selection.Period)
            || !filters.Courses.Any(item => item.Value == selection.Course))
            throw new ArgumentException("Los filtros seleccionados no están disponibles.");
    }

    private static void ValidateIdentity(string user)
    {
        // The catalog SP accepts varchar(20); never silently truncate an audit identity.
        if (string.IsNullOrWhiteSpace(user) || user.Length > 20)
            throw new ArgumentException("No se pudo identificar al usuario para la inscripción.");
    }

    private static void Validate(EnrollmentSelection selection, bool required)
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
