using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilitySettings;

public sealed record GetTeacherAvailabilitySettings(string? Period, string? User)
    : IRequest<ApiResponse<TeacherAvailabilitySettingsDTO>>;

public sealed class GetTeacherAvailabilitySettingsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherAvailabilitySettings, ApiResponse<TeacherAvailabilitySettingsDTO>>
{
    public async Task<ApiResponse<TeacherAvailabilitySettingsDTO>> Handle(
        GetTeacherAvailabilitySettings request, CancellationToken cancellationToken)
    {
        var periods = await procedures.SpRec_PeriodosAcademicosAsync(
            "20082", 0, "NO", "SEMESTRAL", cancellationToken: cancellationToken) ?? [];
        var options = periods
            .Select(row => new TeacherAvailabilityPeriodDTO(
                row.Periodo?.Trim() ?? "",
                row.PeriodoLargo?.Trim() is { Length: > 0 } label ? label : row.Periodo?.Trim() ?? ""))
            .Where(option => option.Value.Length > 0)
            .ToArray();

        var active = await procedures.SpRec_PeriodoDisponibilidadAsync(
            request.User, cancellationToken: cancellationToken) ?? [];
        var activePeriod = active.FirstOrDefault()?.Periodo?.Trim() ?? "";
        if (activePeriod == "Nada") activePeriod = "";

        var period = request.Period?.Trim();
        if (string.IsNullOrEmpty(period)) period = activePeriod;
        if (string.IsNullOrEmpty(period)) period = options.FirstOrDefault()?.Value ?? "";
        if (period.Length > 5) throw new ArgumentException("El período no es válido.");
        if (period.Length == 0)
            return responses.Success(new TeacherAvailabilitySettingsDTO(
                "", activePeriod, options, "", "", "", "", "", ""));

        var data = await procedures.SpRec_PeriodoDisponibilidad_DatosXPeriodoAsync(
            period, cancellationToken: cancellationToken) ?? [];
        var row = data.FirstOrDefault();
        return responses.Success(new TeacherAvailabilitySettingsDTO(
            period,
            activePeriod,
            options,
            Date(row?.FechaInicio),
            Date(row?.FechaTermino),
            Time(row?.HoraIncio, row?.FechaInicio),
            Time(row?.HoraTermino, row?.FechaTermino),
            row?.TextoFechaCorreoDisponibilidad?.Trim() ?? "",
            row?.TextoFechaRezagados?.Trim() ?? ""));
    }

    private static string Date(DateTime? value) =>
        value is null || value.Value.Year <= 1900
            ? "" : value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Time(string? formatted, DateTime? value)
    {
        var text = formatted?.Trim() ?? "";
        if (text.Length > 0) return text;
        return value is null || value.Value.Year <= 1900
            ? "" : value.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
    }
}
