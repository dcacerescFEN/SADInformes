namespace Application.Common.Models;

public record SiteConfigDto
{
    public string? StudentsMenuClientId { get; init; }
    public string? TeachersMenuClientId { get; init; }
    public string? CoursesMenuClientId { get; init; }
    public string? ExamsMenuClientId { get; init; }
    public string? ActivitiesMenuClientId { get; init; }
    public string? AssistantsMenuClientId { get; init; }
    public string? ProcessesMenuClientId { get; init; }
    public string? AdministrationMenuClientId { get; init; }
    public string? ReportsMenuClientId { get; init; }
    public string ReportsMenuUrl { get; init; } = default!;
    public string StudentsMenuUrl { get; init; } = default!;
    public string TeachersMenuUrl { get; init; } = default!;
    public string CoursesMenuUrl { get; init; } = default!;
    public string ExamsMenuUrl { get; init; } = default!;
    public string ActivitiesMenuUrl { get; init; } = default!;
    public string AssistantsMenuUrl { get; init; } = default!;
    public string ProcessesMenuUrl { get; init; } = default!;
    public string AdministrationMenuUrl { get; init; } = default!;
}
