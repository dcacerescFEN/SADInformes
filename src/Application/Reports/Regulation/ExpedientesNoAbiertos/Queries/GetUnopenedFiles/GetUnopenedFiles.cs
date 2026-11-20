using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Regulation.ExpedientesNoAbiertos.Queries.GetUnopenedFiles;

public sealed record GetUnopenedFiles
    : IRequest<ApiResponse<UnopenedFilesReportDTO>>;

public sealed class GetUnopenedFilesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetUnopenedFiles, ApiResponse<UnopenedFilesReportDTO>>
{
    public async Task<ApiResponse<UnopenedFilesReportDTO>> Handle(
        GetUnopenedFiles request, CancellationToken cancellationToken)
    {
        var data = await procedures.SpRec_ExpedientesNoAbiertosAsync(
            cancellationToken: cancellationToken) ?? [];
        var rows = new List<UnopenedFilesRowDTO>(data.Count);
        foreach (var row in data)
        {
            rows.Add(new UnopenedFilesRowDTO(
                Text(row.Codigo), Text(row.Cod_Titulo), Text(row.Rut), Text(row.Nombre),
                Text(row.TituloGrado), Text(row.Carrera), Text(row.Situacion),
                Text(row.Promocion), Text(row.Telefono), Text(row.CorreoFen),
                Text(row.CorreoPersonal)));
        }

        return responses.Success(new UnopenedFilesReportDTO(rows));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
