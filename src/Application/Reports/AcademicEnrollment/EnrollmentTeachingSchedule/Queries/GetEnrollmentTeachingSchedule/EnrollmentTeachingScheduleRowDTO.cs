namespace Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingSchedule;

public sealed record EnrollmentTeachingScheduleRowDTO(
    string Course,
    string CourseName,
    string Teachers,
    string Classes,
    string Assistantships,
    string Room,
    string AssistantRoom,
    string Vacancies,
    string Enrolled,
    string Area,
    string TeacherId);
