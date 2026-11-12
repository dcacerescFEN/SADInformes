using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.FailedMandatoryNotEnrolled.Queries.GetFailedMandatoryNotEnrolled;

public sealed record GetFailedMandatoryNotEnrolled(string? Period)
    : IRequest<ApiResponse<FailedMandatoryNotEnrolledReportDTO>>;

public sealed class GetFailedMandatoryNotEnrolledHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetFailedMandatoryNotEnrolled, ApiResponse<FailedMandatoryNotEnrolledReportDTO>>
{
    public async Task<ApiResponse<FailedMandatoryNotEnrolledReportDTO>> Handle(
        GetFailedMandatoryNotEnrolled request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_ObligatoriasReprobadasNoInscritasSinSolicitudAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new FailedMandatoryNotEnrolledRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Tipo_Alumno),
            Text(row.Cod_Catedra),
            Text(row.Nom_Catedra))).ToArray();

        return responses.Success(new FailedMandatoryNotEnrolledReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
