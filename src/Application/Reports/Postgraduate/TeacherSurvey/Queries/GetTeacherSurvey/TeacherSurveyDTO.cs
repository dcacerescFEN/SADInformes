namespace Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurvey;

/// <summary>Course (A) and teacher (B) evaluation row.</summary>
public sealed record TeacherSurveyDTO(
    string Period, string Course, string Name, string TeacherId, string TeacherName, string Assistants,
    double? A1, double? A2, double? A3, double? A4, string? AComments,
    int? B1, int? B2, int? B3, int? B4, int? B5, int? B6, int? B7,
    int? B8, int? B9, int? B10, int? B11, int? B12, int? B13,
    double? BGrade, string? BComments);
