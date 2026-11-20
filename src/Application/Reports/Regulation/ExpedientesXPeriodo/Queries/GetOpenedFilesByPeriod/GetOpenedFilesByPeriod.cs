using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Regulation.ExpedientesXPeriodo.Queries.GetOpenedFilesByPeriod;

public sealed record GetOpenedFilesByPeriod(string? From, string? To)
    : IRequest<ApiResponse<OpenedFilesByPeriodReportDTO>>;

public sealed class GetOpenedFilesByPeriodHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetOpenedFilesByPeriod, ApiResponse<OpenedFilesByPeriodReportDTO>>
{
    public async Task<ApiResponse<OpenedFilesByPeriodReportDTO>> Handle(
        GetOpenedFilesByPeriod request, CancellationToken cancellationToken)
    {
        var from = Parse(request.From);
        var to = Parse(request.To);
        if (from > to) throw new ArgumentException("El rango de fechas no es válido.");

        // SQL Server interprets yyyy-MM-dd as yyyy-dd-MM for datetime under some
        // dateformat settings; the compact ISO form is unambiguous.
        var start = from.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var end = to.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var data = await procedures.SpRec_ExpedientesAbiertosXPeriodoAsync(
            start, end, cancellationToken: cancellationToken) ?? [];
        var rows = new List<OpenedFilesByPeriodRowDTO>(data.Count);
        foreach (var row in data)
        {
            rows.Add(new OpenedFilesByPeriodRowDTO(
                Text(row.Codigo), Text(row.Cod_Titulo), Text(row.Rut), Text(row.Nombre),
                Text(row.TituloGrado), Text(row.Carrera), Text(row.Situacion),
                Text(row.Promocion), Text(row.FechaApertura), Text(row.Telefono),
                Text(row.CorreoFen), Text(row.CorreoPersonal)));
        }

        return responses.Success(new OpenedFilesByPeriodReportDTO(
            from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), rows));
    }

    private static DateOnly Parse(string? value)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            throw new ArgumentException("El rango de fechas no es válido.");
        }

        return date;
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
