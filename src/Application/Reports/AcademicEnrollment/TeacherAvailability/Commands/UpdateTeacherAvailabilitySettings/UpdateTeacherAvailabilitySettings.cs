using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.UpdateTeacherAvailabilitySettings;

public sealed record UpdateTeacherAvailabilitySettings(
    string? Period, string? StartDate, string? EndDate, string? StartTime, string? EndTime,
    string? PendingEmailText, string? LaggardEmailText, bool ProcessPeriod)
    : IRequest<ApiResponse<bool>>;

public sealed class UpdateTeacherAvailabilitySettingsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<UpdateTeacherAvailabilitySettings, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        UpdateTeacherAvailabilitySettings request, CancellationToken cancellationToken)
    {
        var period = request.Period?.Trim() ?? "";
        if (period.Length is 0 or > 5) throw new ArgumentException("El período no es válido.");

        var start = ParseDate(request.StartDate, "La fecha de inicio no es válida.");
        var end = ParseDate(request.EndDate, "La fecha de término no es válida.");
        var startTime = ParseTime(request.StartTime, "La hora de inicio no es válida.");
        var endTime = ParseTime(request.EndTime, "La hora de término no es válida.");
        if (start.Date + startTime > end.Date + endTime)
            throw new ArgumentException("La fecha de término debe ser posterior a la de inicio.");

        var pending = Text(request.PendingEmailText, "El texto de fecha pendiente no es válido.");
        var laggard = Text(request.LaggardEmailText, "El texto de fecha rezagados no es válido.");

        await procedures.SpMod_PeriodoDisponibilidad_DatosXPeriodoAsync(
            period,
            start.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            startTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
            endTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
            pending,
            request.ProcessPeriod,
            laggard,
            cancellationToken: cancellationToken);
        return responses.Success(true);
    }

    private static DateTime ParseDate(string? value, string message) =>
        DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date : throw new ArgumentException(message);

    private static TimeSpan ParseTime(string? value, string message) =>
        TimeSpan.TryParseExact(value?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var time)
            ? time : throw new ArgumentException(message);

    private static string Text(string? value, string message)
    {
        var text = value?.Trim() ?? "";
        return text.Length <= 100 ? text : throw new ArgumentException(message);
    }
}
