namespace Application.Reports.Postgraduate.SadSigma.Queries.GetSadSigma;

public sealed record SadSigmaDTO(
    string Id, string Cod, string Description, string Area, string Period, string CourseCode,
    string Section, string TeacherName, string PersonCode, string Aqpq, string DedicatedHours,
    string Start, string End, string Promotion, string TeacherType, string CourseName,
    string CourseType, string Program, string Modality, string Confirmed, string ScheduleType,
    string TotalStudents, string PostgraduateStudents, string UndergraduateStudents,
    string TeachingEvaluationAverage, string ExistsInVista, string Scenario1Projected,
    string Scenario1Actual, string Scenario2Projected, string Scenario2Actual,
    string Scenario3Projected, string Scenario3Actual, string ExistsInSigma, string CreatedInSigma,
    string ConfirmationDate, string ConfirmationUser, string CourseObservations,
    string ObservationUser);
