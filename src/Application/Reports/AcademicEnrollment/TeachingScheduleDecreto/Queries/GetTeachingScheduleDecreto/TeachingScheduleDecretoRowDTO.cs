namespace Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Queries.GetTeachingScheduleDecreto;

public sealed record TeachingScheduleDecretoRowDTO(
    string Course,
    string CourseCode,
    string CourseName,
    string Type,
    string Credits,
    string Mention,
    string Requirements,
    string Teachers,
    string Schedule);
