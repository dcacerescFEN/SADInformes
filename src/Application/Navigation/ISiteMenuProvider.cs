namespace Application.Navigation;

public interface ISiteMenuProvider
{
    Task<IReadOnlyDictionary<string, string?>> GetMenuAsync(CancellationToken cancellationToken);
}

public static class SiteMenuFields
{
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[]
    {
        "StudentsMenuUrl", "TeachersMenuUrl", "CoursesMenuUrl", "ExamsMenuUrl",
        "ActivitiesMenuUrl", "AssistantsMenuUrl", "ProcessesMenuUrl", "ReportsMenuUrl", "AdministrationMenuUrl",
        "StudentsMenuClientId", "TeachersMenuClientId", "CoursesMenuClientId", "ExamsMenuClientId",
        "ActivitiesMenuClientId", "AssistantsMenuClientId", "ProcessesMenuClientId", "ReportsMenuClientId", "AdministrationMenuClientId",
    });

    public static IReadOnlyDictionary<string, string> LegacyAliases { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["StudentsMenuUrl"] = "StudentUrl",
            ["TeachersMenuUrl"] = "TeacherUrl",
            ["StudentsMenuClientId"] = "StudentClientId",
            ["TeachersMenuClientId"] = "TeacherClientId",
        };
}
