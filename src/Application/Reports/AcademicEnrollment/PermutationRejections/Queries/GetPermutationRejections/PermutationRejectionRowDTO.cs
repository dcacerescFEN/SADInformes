namespace Application.Reports.AcademicEnrollment.PermutationRejections.Queries.GetPermutationRejections;

public sealed record PermutationRejectionRowDTO(
    string CourseCode,
    string CourseName,
    string Section,
    string StudentType,
    int Rejections);
