using Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudents;
using Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudentsFilters;

namespace Application.Reports.Postgraduate.CoursesByStudents;

/// <summary>Read-only access to the existing SAD procedures used by the report.</summary>
public interface ICoursesByStudentsReader
{
    Task<CoursesByStudentsFiltersDTO> GetFiltersAsync(string? studentType, CancellationToken cancellationToken);

    Task<IReadOnlyList<CoursesByStudentsDTO>> GetRowsAsync(
        string period, string studentType, string modality, CancellationToken cancellationToken);
}
