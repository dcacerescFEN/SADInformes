namespace Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Common;

internal sealed record TeachingScheduleDecretoDefinition(string Slug, string Decree, string Title);

internal static class TeachingScheduleDecretoCatalog
{
    private static readonly TeachingScheduleDecretoDefinition[] Definitions =
    [
        new("teaching-schedule-ic-old-plan", "00921/89%", "Ingeniería Comercial Plan Antiguo"),
        new("teaching-schedule-ic-new-plan", "009615/2003%", "Ingeniería Comercial Plan Nuevo"),
        new("teaching-schedule-iicg-old-plan", "00433/89%",
            "Ingeniería en Información y Control de Gestión Plan Antiguo"),
        new("teaching-schedule-iicg-new-plan", "009538/2003%",
            "Ingeniería en Información y Control de Gestión Plan Nuevo"),
    ];

    public static TeachingScheduleDecretoDefinition Resolve(string? slug)
    {
        var value = slug?.Trim();
        return Definitions.FirstOrDefault(definition =>
            string.Equals(definition.Slug, value, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("La variante de programación docente no es válida.");
    }
}
