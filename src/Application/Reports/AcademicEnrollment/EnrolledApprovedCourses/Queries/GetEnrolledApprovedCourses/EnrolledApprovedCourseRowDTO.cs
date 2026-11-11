namespace Application.Reports.AcademicEnrollment.EnrolledApprovedCourses.Queries.GetEnrolledApprovedCourses;

public sealed record EnrolledApprovedCourseRowDTO(
    string StudentCode,
    string Name,
    string CourseCode,
    string CourseName,
    string Section,
    string Origin);
