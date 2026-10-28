namespace Application.Reports.Postgraduate.UnansweredTeacherSurvey.Queries.GetUnansweredTeacherSurveyFilters;

public sealed record UnansweredTeacherSurveyFiltersDTO(
    IReadOnlyList<UnansweredTeacherSurveyOptionDTO> Periods,
    IReadOnlyList<UnansweredTeacherSurveyOptionDTO> StudentTypes,
    IReadOnlyList<string> Modalities);
