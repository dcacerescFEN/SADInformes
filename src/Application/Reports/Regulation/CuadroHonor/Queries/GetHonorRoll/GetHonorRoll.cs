using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Regulation.CuadroHonor.Common;
using Domain.Common;

namespace Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRoll;

public sealed record GetHonorRoll(string? Career, string? EntryYear, string? HonorYear)
    : IRequest<ApiResponse<HonorRollReportDTO>>;

public sealed class GetHonorRollHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetHonorRoll, ApiResponse<HonorRollReportDTO>>
{
    public async Task<ApiResponse<HonorRollReportDTO>> Handle(
        GetHonorRoll request, CancellationToken cancellationToken)
    {
        var career = HonorRollRules.Career(request.Career);
        var entryYear = HonorRollRules.EntryYear(request.EntryYear);
        var honorYear = HonorRollRules.HonorYear(request.HonorYear);

        var data = await procedures.SpRec_AlumnosCuadroDeHonorAsync(
            career, entryYear, honorYear, cancellationToken: cancellationToken) ?? [];

        var rows = new List<HonorRollRowDTO>(data.Count);
        foreach (var row in data)
        {
            rows.Add(new HonorRollRowDTO(
                row.Posicion, Text(row.Nombre), Text(row.Rut), Text(row.Código),
                Text(row.Ingreso), Text(row.Situación), HonorRollRules.Average(row.Promedio),
                HonorRollRules.Percentile(row.Percentil)));
        }

        return responses.Success(
            new HonorRollReportDTO(career, entryYear, honorYear, rows));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
