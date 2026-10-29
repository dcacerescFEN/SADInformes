namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollment;
public sealed record ExitSurveyEnrollmentDTO(ExitSurveyStudentDTO[] Rows, int Total, bool CanEnroll);
