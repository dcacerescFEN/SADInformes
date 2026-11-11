namespace Application.Reports.AcademicEnrollment.EnrolledApprovedCourses.Queries.GetEnrolledApprovedCourses;

public sealed record EnrolledApprovedCoursesReportDTO(
    string Period,
    EnrolledApprovedCourseRowDTO[] Rows,
    int Total);
