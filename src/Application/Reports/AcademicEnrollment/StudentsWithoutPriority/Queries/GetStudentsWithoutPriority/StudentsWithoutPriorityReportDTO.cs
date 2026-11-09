namespace Application.Reports.AcademicEnrollment.StudentsWithoutPriority.Queries.GetStudentsWithoutPriority;

public sealed record StudentsWithoutPriorityRowDTO(
    string StudentCode,
    string Name,
    string AcademicStatus,
    string StudentType);

public sealed record StudentsWithoutPriorityReportDTO(
    IReadOnlyList<StudentsWithoutPriorityRowDTO> Rows,
    int Total);
