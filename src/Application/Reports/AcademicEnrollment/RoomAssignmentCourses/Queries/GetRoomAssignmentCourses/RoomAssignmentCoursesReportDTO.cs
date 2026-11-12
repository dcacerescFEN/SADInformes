namespace Application.Reports.AcademicEnrollment.RoomAssignmentCourses.Queries.GetRoomAssignmentCourses;

public sealed record RoomAssignmentCoursesReportDTO(
    string Period,
    RoomAssignmentCourseRowDTO[] Rows,
    int Total);
