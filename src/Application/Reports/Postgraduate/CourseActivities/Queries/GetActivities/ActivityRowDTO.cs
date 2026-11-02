namespace Application.Reports.Postgraduate.CourseActivities.Queries.GetActivities;

public sealed record ActivityRowDTO(
    string Id, string Type, string Course, string From, string To, string Period,
    string CourseCode, string Section, string Program, string Modality, string Promotion,
    string Room, string RoomCapacity, string EnrolledStudents, string RoomType, string HybridRoom,
    string EntryDate, string EntryUser, string Suspended, string SuspensionDate, string SuspensionUser,
    string Observations);
