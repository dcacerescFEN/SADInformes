using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValidities;

public sealed record GetTeacherValidities(string? Year)
    : IRequest<ApiResponse<TeacherValiditiesReportDTO>>;

public sealed class GetTeacherValiditiesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherValidities, ApiResponse<TeacherValiditiesReportDTO>>
{
    public async Task<ApiResponse<TeacherValiditiesReportDTO>> Handle(
        GetTeacherValidities request, CancellationToken ct)
    {
        var year = TeacherValiditiesCatalog.RequiredYear(request.Year);
        var filters = await TeacherValiditiesCatalog.ReadAsync(procedures, ct);
        if (!filters.Years.Contains(year))
            throw new ArgumentException("Seleccione un año válido del catálogo.");

        var data = await procedures.SpRec_ProfesoresVigenciasAsync(year, cancellationToken: ct) ?? [];
        // Preserve procedure order and values; do not recalculate hours or validity.
        var rows = data.Select(row => new TeacherValiditiesDTO(
            row.ID?.ToString(CultureInfo.InvariantCulture) ?? "",
            row.Año?.Trim() ?? "", row.Periodo?.Trim() ?? "", row.Cod_Catedra?.Trim() ?? "",
            row.Cod_Seccion?.Trim() ?? "", row.Nom_Catedra?.Trim() ?? "",
            row.Tipo_Alumno?.Trim() ?? "", row.Modalidad?.Trim() ?? "",
            row.Promocion?.Trim() ?? "", row.Cod_Persona?.Trim() ?? "",
            row.Nombre?.Trim() ?? "", row.Mail1?.Trim() ?? "",
            row.HorasDedicadas?.ToString(CultureInfo.InvariantCulture) ?? "", row.Vigencia)).ToArray();
        return responses.Success(new TeacherValiditiesReportDTO(rows, rows.Length));
    }
}
