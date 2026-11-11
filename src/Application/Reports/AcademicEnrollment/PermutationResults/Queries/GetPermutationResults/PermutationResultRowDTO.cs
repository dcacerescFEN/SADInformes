namespace Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationResults;

public sealed record PermutationResultRowDTO(
    string StudentCode,
    string Name,
    string StudentType,
    string CourseCode,
    string CourseName,
    string FromSection,
    string ToSection,
    string Result);
