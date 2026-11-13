using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;

public sealed record GetTeacherAvailability(string? Period, string? User)
    : IRequest<ApiResponse<TeacherAvailabilityReportDTO>>;

public sealed class GetTeacherAvailabilityHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherAvailability, ApiResponse<TeacherAvailabilityReportDTO>>
{
    public async Task<ApiResponse<TeacherAvailabilityReportDTO>> Handle(
        GetTeacherAvailability request, CancellationToken cancellationToken)
    {
        var active = await procedures.SpRec_PeriodoDisponibilidadAsync(
            request.User, cancellationToken: cancellationToken) ?? [];
        var activeRow = active.FirstOrDefault();
        var period = request.Period?.Trim();
        if (string.IsNullOrEmpty(period)) period = activeRow?.Periodo?.Trim() ?? "";
        if (period.Length > 5) throw new ArgumentException("El período no es válido.");
        if (period is "" or "Nada")
            return responses.Success(new TeacherAvailabilityReportDTO("", "", [], 0, 0));

        var data = await procedures.SpRec_FE_ProgramacionDocenteAsync(
            period, cancellationToken: cancellationToken) ?? [];
        var rows = data.Select(row => new TeacherAvailabilityRowDTO(
            Text(row.Codigo),
            Text(row.Catedra),
            Text(row.Cod_Profesor),
            Text(row.Profesor),
            Number(row.Necesarias),
            Number(row.Dadas),
            Text(row.Disp),
            Text(row.FechaCreacion),
            Text(row.Estado),
            Text(row.Mail),
            Number(row.CorreoBienvenida),
            Text(row.Observaciones))).ToArray();
        var pending = rows.Count(row => string.Equals(row.State, "PENDIENTE", StringComparison.OrdinalIgnoreCase));
        var periodName = string.Equals(activeRow?.Periodo?.Trim(), period, StringComparison.OrdinalIgnoreCase)
            ? Text(activeRow?.NombrePeriodo) : period;

        return responses.Success(new TeacherAvailabilityReportDTO(
            period, periodName, rows, pending, rows.Length - pending));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
}
