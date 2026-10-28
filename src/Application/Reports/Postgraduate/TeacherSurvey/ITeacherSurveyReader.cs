using Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurvey;
using Application.Reports.Postgraduate.TeacherSurvey.Queries.GetTeacherSurveyFilters;

namespace Application.Reports.Postgraduate.TeacherSurvey;

/// <summary>Read-only access to the existing SAD procedures used by the report.</summary>
public interface ITeacherSurveyReader
{
    Task<TeacherSurveyFiltersDTO> GetFiltersAsync(string? studentType, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherSurveyDTO>> GetRowsAsync(
        string period, string studentType, string modality, CancellationToken cancellationToken);
}
