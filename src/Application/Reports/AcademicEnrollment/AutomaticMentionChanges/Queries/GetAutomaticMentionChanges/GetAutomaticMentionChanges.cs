using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.AutomaticMentionChanges.Queries.GetAutomaticMentionChanges;

public sealed record GetAutomaticMentionChanges(string? Period)
    : IRequest<ApiResponse<AutomaticMentionChangesReportDTO>>;

public sealed class GetAutomaticMentionChangesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetAutomaticMentionChanges, ApiResponse<AutomaticMentionChangesReportDTO>>
{
    public async Task<ApiResponse<AutomaticMentionChangesReportDTO>> Handle(
        GetAutomaticMentionChanges request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_AlumnosCambioMencionAutomaticoAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new AutomaticMentionChangeRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Mencion))).ToArray();

        return responses.Success(new AutomaticMentionChangesReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
