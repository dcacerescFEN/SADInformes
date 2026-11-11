using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.PermutationResults.Queries.GetPermutationResults;

public sealed record GetPermutationResults(string? Period, string? From, string? To)
    : IRequest<ApiResponse<PermutationResultsReportDTO>>;

public sealed class GetPermutationResultsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPermutationResults, ApiResponse<PermutationResultsReportDTO>>
{
    public async Task<ApiResponse<PermutationResultsReportDTO>> Handle(
        GetPermutationResults request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var from = PermutationRules.Date(request.From, "desde");
        var to = PermutationRules.Date(request.To, "hasta");
        if (from > to) throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.");

        var data = await procedures.SpRec_ResPermutaXAlumnoAsync(
            period, from, to, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new PermutationResultRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Tipo),
            Text(row.Cod_Catedra),
            Text(row.Nom_Catedra),
            Text(row.Desde),
            Text(row.Hacia),
            Text(row.Estado))).ToArray();

        return responses.Success(new PermutationResultsReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
