using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;

public sealed record GetTeacherAvailabilityObservations(string? Period)
    : IRequest<ApiResponse<TeacherAvailabilityObservationsReportDTO>>;

public sealed class GetTeacherAvailabilityObservationsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherAvailabilityObservations, ApiResponse<TeacherAvailabilityObservationsReportDTO>>
{
    public async Task<ApiResponse<TeacherAvailabilityObservationsReportDTO>> Handle(
        GetTeacherAvailabilityObservations request, CancellationToken cancellationToken)
    {
        var periods = await procedures.SpRec_PeriodosAcademicosAsync(
            "20082", 0, "NO", "SEMESTRAL", cancellationToken: cancellationToken) ?? [];
        var options = periods.Select(row => new TeacherAvailabilityPeriodDTO(
            row.Periodo?.Trim() ?? "",
            row.PeriodoLargo?.Trim() is { Length: > 0 } label ? label : row.Periodo?.Trim() ?? ""))
            .Where(option => option.Value.Length > 0).ToArray();
        var period = request.Period?.Trim();
        if (string.IsNullOrEmpty(period)) period = options.FirstOrDefault()?.Value ?? "";
        if (period.Length > 5) throw new ArgumentException("El período no es válido.");
        if (period.Length == 0)
            return responses.Success(new TeacherAvailabilityObservationsReportDTO("", options, []));

        var data = await procedures.SpRec_FE_DisponibilidadObservacionesAsync(
            period, cancellationToken: cancellationToken) ?? [];
        var rows = data.Select(row => new TeacherAvailabilityObservationRowDTO(
            row.Cod_Profesor?.Trim() ?? "",
            row.Profesor?.Trim() ?? "",
            row.Observaciones?.Trim() ?? "")).ToArray();
        return responses.Success(new TeacherAvailabilityObservationsReportDTO(period, options, rows));
    }
}
