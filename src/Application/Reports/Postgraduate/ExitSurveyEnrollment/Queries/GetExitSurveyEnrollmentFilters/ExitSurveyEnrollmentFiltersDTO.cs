namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollmentFilters;
public sealed record ExitSurveyEnrollmentFiltersDTO(ExitSurveyOptionDTO[] StudentTypes, ExitSurveyOptionDTO[] Modalities, string[] Years);
