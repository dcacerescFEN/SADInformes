using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.Licenciatura.Queries.GetLicenciatura;

public sealed record GetLicenciatura : IRequest<ApiResponse<LicenciaturaReportDTO>>;

public sealed class GetLicenciaturaHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetLicenciatura, ApiResponse<LicenciaturaReportDTO>>
{
    public async Task<ApiResponse<LicenciaturaReportDTO>> Handle(
        GetLicenciatura request, CancellationToken ct)
    {
        var data = await procedures.SpList_AlumnosCumpleLicenciaturaAsync(cancellationToken: ct) ?? [];
        var rows = data.Select(row => new LicenciaturaRowDTO(
            Text(row.Cod_Alumno), Text(row.Nombre), Text(row.NombreCarrera),
            Text(row.TituloGrado), Text(row.Estado))).ToArray();
        return responses.Success(new LicenciaturaReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
