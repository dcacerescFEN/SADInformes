using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.TeacherSurvey;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherSurvey.Queries;

public sealed record TeacherSurveyFiltersDto(
    IReadOnlyList<TeacherSurveyOption> Periods,
    IReadOnlyList<TeacherSurveyOption> StudentTypes,
    IReadOnlyList<string> Modalities);

public sealed record TeacherSurveyReportDto(
    IReadOnlyList<TeacherSurveyRow> Rows,
    int Total);

public sealed record GetTeacherSurveyFilters(string? StudentType)
    : IRequest<ApiResponse<TeacherSurveyFiltersDto>>;

public sealed record GetTeacherSurvey(string Period, string StudentType, string Modality)
    : IRequest<ApiResponse<TeacherSurveyReportDto>>;

public sealed class GetTeacherSurveyFiltersHandler(ITeacherSurveyReader reader, IApiResponseService responses)
    : IRequestHandler<GetTeacherSurveyFilters, ApiResponse<TeacherSurveyFiltersDto>>
{
    public async Task<ApiResponse<TeacherSurveyFiltersDto>> Handle(
        GetTeacherSurveyFilters request, CancellationToken cancellationToken)
    {
        var filters = await reader.GetFiltersAsync(request.StudentType?.Trim(), cancellationToken);
        return responses.Success(new TeacherSurveyFiltersDto(filters.Periods, filters.StudentTypes, filters.Modalities));
    }
}

public sealed class GetTeacherSurveyHandler(ITeacherSurveyReader reader, IApiResponseService responses)
    : IRequestHandler<GetTeacherSurvey, ApiResponse<TeacherSurveyReportDto>>
{
    public async Task<ApiResponse<TeacherSurveyReportDto>> Handle(
        GetTeacherSurvey request, CancellationToken cancellationToken)
    {
        var period = TeacherSurveyFiltersValidation.Required("El período es obligatorio.", request.Period);
        var studentType = TeacherSurveyFiltersValidation.Required("Seleccione un tipo de alumno.", request.StudentType);
        var modality = TeacherSurveyFiltersValidation.Required("Seleccione una modalidad.", request.Modality);

        var rows = await reader.GetRowsAsync(period, studentType, modality, cancellationToken);
        return responses.Success(new TeacherSurveyReportDto(rows, rows.Count));
    }
}

/// <summary>
/// El procedimiento existente compara tipo y modalidad con igualdad, así que un valor
/// comodín no filtra: devolvería una tabla vacía. Se rechaza explícitamente.
/// </summary>
public static class TeacherSurveyFiltersValidation
{
    private static readonly string[] Wildcards = ["Todos", "%", "_", "*"];

    public static string Required(string message, string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || Wildcards.Any(wildcard =>
                string.Equals(trimmed, wildcard, StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains('%', StringComparison.Ordinal)
                || trimmed.Contains('_', StringComparison.Ordinal)))
            throw new ArgumentException(message);
        return trimmed;
    }
}
