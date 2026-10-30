using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.SadSigma.Queries.GetSadSigma;

public sealed record GetSadSigma(string? Year) : IRequest<ApiResponse<SadSigmaReportDTO>>;

public sealed class GetSadSigmaHandler(
    ISadContextProcedures procedures, ISigmaRemuneracionesContext sigma, IApiResponseService responses)
    : IRequestHandler<GetSadSigma, ApiResponse<SadSigmaReportDTO>>
{
    public async Task<ApiResponse<SadSigmaReportDTO>> Handle(GetSadSigma request, CancellationToken ct)
    {
        var year = SadSigmaCatalog.RequiredYear(request.Year);
        var filters = await SadSigmaCatalog.ReadAsync(procedures, ct);
        if (!filters.Years.Contains(year))
            throw new ArgumentException("Seleccione un año válido del catálogo.");

        var data = await procedures.SpRec_CatedrasAreasXPagosProfesoresAsync(year, cancellationToken: ct) ?? [];
        var codes = data.Select(row => row.COD?.Trim()).Where(code => !string.IsNullOrEmpty(code))
            .Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sigmaDates = await ReadSigmaAsync(sigma, codes, ct);
        var rows = data.Select(row =>
        {
            var code = row.COD?.Trim();
            DateTime? createdDate = null;
            var found = !string.IsNullOrEmpty(code) && sigmaDates.TryGetValue(code, out createdDate);
            return new SadSigmaDTO(
                Number(row.ID), Text(row.COD), Text(row.descripcion), Text(row.area), Text(row.periodo),
                Text(row.cod_catedra), Text(row.cod_seccion), Text(row.Nombre_Profesor),
                Text(row.Cod_Persona), Text(row.AQPQ), Number(row.HorasDedicadas), Text(row.Inicio),
                Text(row.Termino), Text(row.Promocion), Text(row.Tipo_Profesor), Text(row.nom_catedra),
                Text(row.TipoCatedra), Text(row.Programa), Text(row.Modalidad), Text(row.Confirmado),
                Text(row.Tipo_Horario), Number(row.Alumnos_Totales), Number(row.Alumnos_Postgrado),
                Number(row.Alumnos_Pregrado), Text(row.Promedio_Ev_Docente), Text(row.Existe_En_Vista),
                Number(row.Escenario1Proyectado), Number(row.Escenario1Real),
                Number(row.Escenario2Proyectado), Number(row.Escenario2Real),
                Number(row.Escenario3Proyectado), Number(row.Escenario3Real),
                found ? "SI" : "NO", Date(createdDate), Date(row.Fecha_Confirmacion),
                Text(row.Usuario_Confirmacion), Text(row.Observaciones_Curso), Text(row.Usuario_Observ));
        }).ToArray();
        return responses.Success(new SadSigmaReportDTO(rows, rows.Length));
    }

    private static async Task<IReadOnlyDictionary<string, DateTime?>> ReadSigmaAsync(
        ISigmaRemuneracionesContext sigma, IReadOnlyCollection<string> codes, CancellationToken ct)
    {
        var empty = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
        if (!sigma.IsConfigured || codes.Count == 0) return empty;
        try
        {
            var requested = codes.ToList();
            var rows = await (from prestacion in sigma.Prestaciones.AsNoTracking()
                              join remuneracion in sigma.Remuneraciones.AsNoTracking()
                                  on prestacion.RemuneracionId equals remuneracion.Id
                              where prestacion.CodImp != null && requested.Contains(prestacion.CodImp)
                              group remuneracion.CreatedAt by prestacion.CodImp into grouped
                              select new { Code = grouped.Key!, CreatedAt = grouped.Max() })
                .ToListAsync(ct);
            return rows.Where(row => !string.IsNullOrWhiteSpace(row.Code))
                .ToDictionary(row => row.Code, row => row.CreatedAt, StringComparer.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException) { throw; }
        catch { return empty; }
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Number(double? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Date(DateTime? value) =>
        value?.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}
