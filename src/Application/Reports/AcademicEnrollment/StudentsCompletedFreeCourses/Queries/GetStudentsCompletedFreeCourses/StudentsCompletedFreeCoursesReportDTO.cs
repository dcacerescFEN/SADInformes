namespace Application.Reports.AcademicEnrollment.StudentsCompletedFreeCourses.Queries.GetStudentsCompletedFreeCourses;

public sealed record StudentsCompletedFreeCoursesReportDTO(
    string Period,
    StudentCompletedFreeCourseRowDTO[] Rows,
    int Total);
