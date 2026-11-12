using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Queries.GetTeachingScheduleDecretoFilters;

public sealed record GetTeachingScheduleDecretoFilters(string? Slug)
    : IRequest<ApiResponse<TeachingScheduleDecretoFiltersDTO>>;

public sealed class GetTeachingScheduleDecretoFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeachingScheduleDecretoFilters, ApiResponse<TeachingScheduleDecretoFiltersDTO>>
{
    public async Task<ApiResponse<TeachingScheduleDecretoFiltersDTO>> Handle(
        GetTeachingScheduleDecretoFilters request, CancellationToken cancellationToken)
    {
        var definition = TeachingScheduleDecretoCatalog.Resolve(request.Slug);
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "PREGRADO", cancellationToken: cancellationToken) ?? [];

        return responses.Success(new TeachingScheduleDecretoFiltersDTO(
            definition.Title,
            periods.Select(row => new TeachingScheduleDecretoOptionDTO(
                row.Periodo.Trim(),
                row.PeriodoLargo?.Trim() is { Length: > 0 } label ? label : row.Periodo.Trim()))
                .Where(option => option.Value.Length > 0)
                .DistinctBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }
}
