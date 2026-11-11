using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.EnrolledWithoutRequirements.Queries.GetEnrolledWithoutRequirements;

public sealed record GetEnrolledWithoutRequirements(string? Period)
    : IRequest<ApiResponse<EnrolledWithoutRequirementsReportDTO>>;

public sealed class GetEnrolledWithoutRequirementsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetEnrolledWithoutRequirements, ApiResponse<EnrolledWithoutRequirementsReportDTO>>
{
    public async Task<ApiResponse<EnrolledWithoutRequirementsReportDTO>> Handle(
        GetEnrolledWithoutRequirements request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_InscritasSinRequisitosAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new EnrolledWithoutRequirementsRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Cod_Catedra),
            Text(row.Nom_Catedra),
            Text(row.Cod_Seccion),
            Text(row.Origen),
            Text(row.Motivo),
            Text(row.Fecha),
            Text(row.Usuario))).ToArray();

        return responses.Success(new EnrolledWithoutRequirementsReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
