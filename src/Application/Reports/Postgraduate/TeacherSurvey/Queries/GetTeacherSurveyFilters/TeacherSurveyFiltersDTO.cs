namespace Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurveyFilters;

public sealed record TeacherSurveyFiltersDTO(
    IReadOnlyList<TeacherSurveyOptionDTO> Periods,
    IReadOnlyList<TeacherSurveyOptionDTO> StudentTypes,
    IReadOnlyList<string> Modalities);
