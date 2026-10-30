using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.AyudantiaSigma.Queries.GetAyudantiaSigma;

public sealed record GetAyudantiaSigma(string? Year) : IRequest<ApiResponse<AyudantiaSigmaReportDTO>>;

public sealed class GetAyudantiaSigmaHandler(
    ISadContext sad, ISigmaRemuneracionesContext sigma, ISadContextProcedures procedures,
    IApiResponseService responses)
    : IRequestHandler<GetAyudantiaSigma, ApiResponse<AyudantiaSigmaReportDTO>>
{
    public async Task<ApiResponse<AyudantiaSigmaReportDTO>> Handle(
        GetAyudantiaSigma request, CancellationToken ct)
    {
        var year = AyudantiaSigmaCatalog.RequiredYear(request.Year);
        var filters = await AyudantiaSigmaCatalog.ReadAsync(procedures, ct);
        if (!filters.Years.Contains(year))
            throw new ArgumentException("Seleccione un año válido del catálogo.");
        var data = await sad.PostgradoAyudantesSigmaSinHistorial.AsNoTracking()
            .Where(row => row.AñoCatedra == year).ToListAsync(ct);
        var codes = data.Select(row => row.COD?.Trim())
            .Where(code => !string.IsNullOrEmpty(code)).Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sigmaRows = await ReadSigmaAsync(sigma, codes, ct);
        var rows = data.Select(row =>
        {
            var code = row.COD?.Trim();
            var found = !string.IsNullOrEmpty(code)
                && sigmaRows.TryGetValue(code, out var info) && info is not null;
            DateTime? sigmaDate = null;
            var step = ""; var flag = ""; var remunerationId = "";
            if (found && sigmaRows[code!] is { } value)
            {
                sigmaDate = value.CreatedAt;
                step = Step(value.EstadoId);
                flag = Flag(value.BanderaId);
                remunerationId = value.Id.ToString(CultureInfo.InvariantCulture);
            }
            return new AyudantiaSigmaDTO(
                Text(row.COD), Text(row.Nombre1), Text(row.Nombre2), Text(row.Apellido1),
                Text(row.Apellido2), Text(row.CodPersona), Text(row.Email), Text(row.CodCatedra),
                Text(row.NomCatedra), Text(row.CodSeccion), Date(row.Inicio), Date(row.Termino),
                Text(row.AñoCatedra), Text(row.Periodo), Text(row.TipoAlumnoPostgrado),
                Text(row.ModalidadPostgrado), Text(row.Promocion), Text(row.Ccosto),
                Text(row.TipoPago), Number(row.ValorCursoReal), found ? "SI" : "NO",
                Date(sigmaDate), step, flag, remunerationId);
        }).ToArray();
        return responses.Success(new AyudantiaSigmaReportDTO(rows, rows.Length));
    }

    private static async Task<Dictionary<string, SigmaInfo>> ReadSigmaAsync(
        ISigmaRemuneracionesContext sigma, IReadOnlyCollection<string> codes, CancellationToken ct)
    {
        var empty = new Dictionary<string, SigmaInfo>(StringComparer.OrdinalIgnoreCase);
        if (!sigma.IsConfigured || codes.Count == 0) return empty;
        try
        {
            var requested = codes.ToList();
            var rows = await (from prestacion in sigma.Prestaciones.AsNoTracking()
                              join remuneracion in sigma.Remuneraciones.AsNoTracking()
                                  on prestacion.RemuneracionId equals remuneracion.Id
                              where prestacion.CodImp != null && requested.Contains(prestacion.CodImp)
                              orderby prestacion.CodImp, remuneracion.Id
                              select new { Code = prestacion.CodImp!, remuneracion.Id,
                                  remuneracion.CreatedAt, remuneracion.EstadoId, remuneracion.BanderaId })
                .ToListAsync(ct);
            var result = new Dictionary<string, SigmaInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
                result[row.Code] = new SigmaInfo(row.Id, row.CreatedAt, row.EstadoId, row.BanderaId);
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch { return empty; }
    }

    private static string Step(float state) => Math.Floor(state) switch
    {
        0 => "PASO 1", 1 or 2 => "PASO 2", 3 => "ERROR", 4 => "HDR", 5 => "PASO 3",
        6 => "PASO 4", >= 7 and <= 10 => "PAGO", 21 or 22 => "ELIMINADA", _ => "DESCONOCIDO",
    };

    private static string Flag(bool flag) => flag ? "🚩" : "⛔";
    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Date(DateOnly? value) => value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
    private static string Date(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
    private sealed record SigmaInfo(int Id, DateTime? CreatedAt, float EstadoId, bool BanderaId);
}
