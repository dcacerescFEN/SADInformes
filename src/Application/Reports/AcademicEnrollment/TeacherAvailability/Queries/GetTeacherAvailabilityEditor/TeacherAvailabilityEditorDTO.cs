namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityEditor;

public sealed record TeacherAvailabilityEditorDTO(
    string Period,
    string TeacherCode,
    string TeacherName,
    IReadOnlyList<TeacherAvailabilityEditorCourseDTO> Courses,
    IReadOnlyList<TeacherAvailabilityEditorModuleTypeDTO> ModuleTypes,
    IReadOnlyList<TeacherAvailabilityEditorModuleDTO> Modules,
    IReadOnlyList<TeacherAvailabilityEditorModuleOptionDTO> ModuleOptions);

public sealed record TeacherAvailabilityEditorCourseDTO(
    string Code, string Section, string Name, string Description, int ModuleType);

public sealed record TeacherAvailabilityEditorModuleTypeDTO(int Value, string Text);

public sealed record TeacherAvailabilityEditorModuleDTO(int Id, int ModuleType, string ModuleCascade);

public sealed record TeacherAvailabilityEditorModuleOptionDTO(int ModuleType, string Value);
