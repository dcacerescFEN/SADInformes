namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.SaveTeacherAvailabilityEditor;

public sealed record SaveTeacherAvailabilityEditorRequestDTO(
    string? Period,
    string? TeacherCode,
    IReadOnlyList<TeacherAvailabilityEditorCourseUpdateDTO>? Courses,
    IReadOnlyList<TeacherAvailabilityEditorModuleUpdateDTO>? Modules,
    IReadOnlyList<TeacherAvailabilityEditorModuleAddDTO>? NewModules);

public sealed record TeacherAvailabilityEditorCourseUpdateDTO(
    string? Code, string? Section, int ModuleType);

public sealed record TeacherAvailabilityEditorModuleUpdateDTO(int Id, string? ModuleCascade);

public sealed record TeacherAvailabilityEditorModuleAddDTO(int ModuleType, string? ModuleCascade);
