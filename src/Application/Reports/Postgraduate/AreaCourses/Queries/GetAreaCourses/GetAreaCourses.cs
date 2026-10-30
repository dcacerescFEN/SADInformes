using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCourses;

public sealed record GetAreaCourses(string? Year1, string? Year2)
    : IRequest<ApiResponse<AreaCoursesReportDTO>>;

public sealed class GetAreaCoursesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetAreaCourses, ApiResponse<AreaCoursesReportDTO>>
{
    public async Task<ApiResponse<AreaCoursesReportDTO>> Handle(
        GetAreaCourses request, CancellationToken ct)
    {
        var year1 = AreaCoursesCatalog.RequiredYear(request.Year1, "Seleccione el primer año.");
        var year2 = AreaCoursesCatalog.RequiredYear(request.Year2, "Seleccione el segundo año.");
        var filters = await AreaCoursesCatalog.ReadAsync(procedures, ct);
        if (!filters.Years.Contains(year1) || !filters.Years.Contains(year2))
            throw new ArgumentException("Seleccione años válidos del catálogo.");

        var data = await procedures.SpRec_CatedrasAreas_ReportePostgradoAsync(
            year1, year2, cancellationToken: ct) ?? [];
        // Preserve procedure order and all formatted text; do not recalculate the report.
        var rows = data.Select(row => new AreaCoursesDTO(
            row.ID?.ToString(CultureInfo.InvariantCulture) ?? "",
            Text(row.descripcion), Text(row.area), Text(row.periodo), Text(row.cod_catedra),
            Text(row.cod_seccion), Number(row.Creditos), Text(row.Nombre_Profesor),
            Text(row.Cod_Persona), Text(row.Tipo_Profesor), Text(row.Inicio), Text(row.Termino),
            Text(row.Programa), Text(row.Modalidad), Text(row.Promocion), Text(row.nom_catedra),
            Text(row.TipoCatedra), Text(row.Tipo_Horario), Text(row.AQPQ),
            Text(row.FacultyQualificationSA_PA_SP_IP), Text(row.Confirmado),
            Number(row.HorasDedicadas), Number(row.Alumnos_Totales), Number(row.Alumnos_Postgrado),
            Number(row.Alumnos_Pregrado), Text(row.Promedio_Ev_Docente),
            Text(row.Observaciones_Curso), Text(row.Usuario_Observ))).ToArray();
        return responses.Success(new AreaCoursesReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Number(double? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Number(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "";
}
