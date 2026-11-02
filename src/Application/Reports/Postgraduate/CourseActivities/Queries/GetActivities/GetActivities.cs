using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.CourseActivities.Queries.GetActivities;

public sealed record GetActivities(string? Year) : IRequest<ApiResponse<ActivitiesReportDTO>>;

public sealed class GetActivitiesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetActivities, ApiResponse<ActivitiesReportDTO>>
{
    public async Task<ApiResponse<ActivitiesReportDTO>> Handle(GetActivities request, CancellationToken ct)
    {
        var yearRows = await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [];
        var years = yearRows.Select(row => row.Año?.Trim())
            .Where(value => value is { Length: 4 } && value.All(char.IsAsciiDigit))
            .Cast<string>().ToArray();
        var year = string.IsNullOrWhiteSpace(request.Year) ? years.FirstOrDefault() : request.Year.Trim();
        if (year is null || !years.Contains(year)
            || !int.TryParse(year, NumberStyles.None, CultureInfo.InvariantCulture, out var anno))
            throw new ArgumentException("El año no es válido.");
        var data = await procedures.SpRec_ReporteActividadesCursosPostgradoAsync(anno, cancellationToken: ct) ?? [];
        var rows = data.Select(row => new ActivityRowDTO(
            row.Id.ToString(CultureInfo.InvariantCulture), Text(row.Tipo), Text(row.Catedra),
            Text(row.Desde), Text(row.Hasta), Text(row.Periodo), Text(row.Cod_Catedra),
            Text(row.Cod_Seccion), Text(row.Programa), Text(row.Modalidad), Text(row.Promocion),
            Text(row.Sala), Number(row.Capacidad_Sala), Number(row.Estudiantes_Inscritos),
            Text(row.Tipo_Sala), Text(row.Es_Sala_Hibrida), Text(row.Fecha_Ingreso),
            Text(row.UsuarioIngreso), Text(row.Suspendida), Date(row.Fecha_Suspención),
            Text(row.Usuario_Suspención), Text(row.Observaciones))).ToArray();
        return responses.Success(new ActivitiesReportDTO(year, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Date(DateTime? value) =>
        value?.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}
