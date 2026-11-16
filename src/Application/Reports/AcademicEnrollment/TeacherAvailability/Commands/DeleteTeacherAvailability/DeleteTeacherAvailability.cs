using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.DeleteTeacherAvailability;

public sealed record DeleteTeacherAvailability(string? Period, string? TeacherCode)
    : IRequest<ApiResponse<bool>>;

public sealed class DeleteTeacherAvailabilityHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<DeleteTeacherAvailability, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteTeacherAvailability request, CancellationToken cancellationToken)
    {
        var period = request.Period?.Trim() ?? "";
        var teacherCode = request.TeacherCode?.Trim() ?? "";
        if (period.Length is 0 or > 5) throw new ArgumentException("El período no es válido.");
        if (teacherCode.Length is 0 or > 15) throw new ArgumentException("El profesor no es válido.");

        await procedures.SpELI_DisponibilidadesAsync(
            teacherCode, period, cancellationToken: cancellationToken);
        return responses.Success(true);
    }
}
