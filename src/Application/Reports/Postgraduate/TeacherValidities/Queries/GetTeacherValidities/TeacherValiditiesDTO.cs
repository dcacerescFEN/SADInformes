namespace Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValidities;

public sealed record TeacherValiditiesDTO(
    string Id, string Year, string Period, string CourseCode, string Section, string CourseName,
    string StudentType, string Modality, string Promotion, string PersonCode, string Name,
    string Email, string DedicatedHours, bool Valid);
