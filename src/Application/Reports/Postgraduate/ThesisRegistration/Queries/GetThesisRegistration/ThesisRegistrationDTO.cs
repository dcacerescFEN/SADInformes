namespace Application.Reports.Postgraduate.ThesisRegistration.Queries.GetThesisRegistration;

public sealed record ThesisRegistrationDTO(
    string StudentCode, string Enrollment, string EntrySemester, string Decree, string PersonCode,
    string StudentType, string Modality, string Surnames, string Names, string AfeCourse,
    string AfePeriod, string AfeTeacher, string AfeGrade, string CourseCode, string ThesisEnrollmentPeriod,
    string AcademicSituation, string Reason, string ThesisName, string Grade, string Teachers,
    string ExamDate);
