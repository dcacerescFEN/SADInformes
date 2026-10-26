namespace Application.Navigation;

public interface ISiteMenuProvider
{
    Task<IReadOnlyDictionary<string, string?>> GetMenuAsync(CancellationToken cancellationToken);
}

public static class SiteMenuFields
{
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[]
    {
        "StudentUrl", "TeacherUrl", "CoursesMenuUrl", "ExamsMenuUrl",
        "ActivitiesMenuUrl", "AssistantsMenuUrl", "ProcessesMenuUrl", "AdministrationMenuUrl",
        "StudentClientId", "TeacherClientId", "CoursesMenuClientId", "ExamsMenuClientId",
        "ActivitiesMenuClientId", "AssistantsMenuClientId", "ProcessesMenuClientId", "AdministrationMenuClientId",
    });
}
