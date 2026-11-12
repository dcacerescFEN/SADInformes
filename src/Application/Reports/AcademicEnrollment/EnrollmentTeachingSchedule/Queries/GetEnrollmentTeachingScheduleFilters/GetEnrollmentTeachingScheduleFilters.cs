using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingScheduleFilters;

public sealed record GetEnrollmentTeachingScheduleFilters(string? School)
    : IRequest<ApiResponse<EnrollmentTeachingScheduleFiltersDTO>>;

public sealed class GetEnrollmentTeachingScheduleFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetEnrollmentTeachingScheduleFilters, ApiResponse<EnrollmentTeachingScheduleFiltersDTO>>
{
    public async Task<ApiResponse<EnrollmentTeachingScheduleFiltersDTO>> Handle(
        GetEnrollmentTeachingScheduleFilters request, CancellationToken cancellationToken)
    {
        var school = EnrollmentTeachingScheduleRules.School(request.School);
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", school, cancellationToken: cancellationToken) ?? [];
        var areas = await procedures.SpRec_AreasCatedrasAsync(
            -1, 0, cancellationToken: cancellationToken) ?? [];

        return responses.Success(new EnrollmentTeachingScheduleFiltersDTO(
            periods.Select(row => new EnrollmentTeachingScheduleOptionDTO(
                row.Periodo.Trim(),
                row.PeriodoLargo?.Trim() is { Length: > 0 } label ? label : row.Periodo.Trim()))
                .Where(option => option.Value.Length > 0)
                .DistinctBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            areas.Select(row => new EnrollmentTeachingScheduleOptionDTO(
                row.Codigo?.Trim() ?? "",
                row.Descripcion?.Trim() is { Length: > 0 } label ? label : row.Codigo?.Trim() ?? ""))
                .Where(option => option.Value.Length > 0)
                .DistinctBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }
}
