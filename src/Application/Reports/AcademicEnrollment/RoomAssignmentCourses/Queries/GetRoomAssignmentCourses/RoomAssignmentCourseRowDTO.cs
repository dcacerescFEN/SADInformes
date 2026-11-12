namespace Application.Reports.AcademicEnrollment.RoomAssignmentCourses.Queries.GetRoomAssignmentCourses;

public sealed record RoomAssignmentCourseRowDTO(
    string Period,
    string CourseCode,
    string Section,
    string Deleted,
    string Classes,
    string Type,
    string OfferedVacancies,
    string OccupiedVacancies,
    string Teacher1,
    string Teacher2,
    string Teacher3,
    string Teacher4,
    string Teacher5,
    string Teacher6,
    string Teacher7,
    string Teacher8,
    string Teacher9,
    string Teacher10);
