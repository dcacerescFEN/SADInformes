using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityEditor;

public sealed record GetTeacherAvailabilityEditor(string? Period, string? TeacherCode)
    : IRequest<ApiResponse<TeacherAvailabilityEditorDTO>>;

public sealed class GetTeacherAvailabilityEditorHandler(
    ISadContext sad, IDocenciaWebContext docencia,
    IDocenciaWebProcedures docenciaProcedures, IApiResponseService responses)
    : IRequestHandler<GetTeacherAvailabilityEditor, ApiResponse<TeacherAvailabilityEditorDTO>>
{
    public async Task<ApiResponse<TeacherAvailabilityEditorDTO>> Handle(
        GetTeacherAvailabilityEditor request, CancellationToken cancellationToken)
    {
        var period = request.Period?.Trim() ?? "";
        var teacherCode = request.TeacherCode?.Trim() ?? "";
        if (period.Length is 0 or > 5) throw new ArgumentException("El período no es válido.");
        if (teacherCode.Length is 0 or > 15) throw new ArgumentException("El profesor no es válido.");

        var courses = await docenciaProcedures.SpRec_FE_CatedrasAsync(
            teacherCode, period, cancellationToken: cancellationToken) ?? [];
        var types = await docencia.FeTipoModulos.AsNoTracking()
            .OrderBy(row => row.TipoModulo)
            .Select(row => new TeacherAvailabilityEditorModuleTypeDTO(row.TipoModulo, row.Descripcion))
            .ToListAsync(cancellationToken);
        var modules = await docencia.FeModulosCascadaFinal.AsNoTracking()
            .Where(row => row.Periodo == period && row.CodPersona == teacherCode
                && (row.Eliminada == null || row.Eliminada == false))
            .OrderBy(row => row.Id)
            .Select(row => new TeacherAvailabilityEditorModuleDTO(
                row.Id, row.TipoModulo, row.ModuloCascada))
            .ToListAsync(cancellationToken);

        var options = new List<TeacherAvailabilityEditorModuleOptionDTO>();
        foreach (var type in types)
        {
            var rows = await docenciaProcedures.SpRec_FE_ModulosAsync(
                type.Value, cancellationToken: cancellationToken) ?? [];
            options.AddRange(rows
                .Where(row => !string.IsNullOrWhiteSpace(row.Modulo))
                .Select(row => new TeacherAvailabilityEditorModuleOptionDTO(
                    row.TipoModulo ?? type.Value, row.Modulo!.Trim())));
        }

        var person = await sad.Personas.AsNoTracking()
            .Where(row => row.CodPersona == teacherCode)
            .Select(row => new { row.Nombre1, row.Nombre2, row.Apellido1, row.Apellido2 })
            .FirstOrDefaultAsync(cancellationToken);
        var teacherName = person is null ? teacherCode : string.Join(' ', new[]
        {
            person.Nombre1, person.Nombre2, person.Apellido1, person.Apellido2,
        }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();

        return responses.Success(new TeacherAvailabilityEditorDTO(
            period, teacherCode, teacherName,
            courses.Select(row => new TeacherAvailabilityEditorCourseDTO(
                row.Cod_Catedra?.Trim() ?? "", row.Cod_Seccion?.Trim() ?? "",
                row.Nom_Catedra?.Trim() ?? "", row.Descripcion?.Trim() ?? "",
                row.Tipo_Modulo ?? 0)).ToArray(),
            types, modules, options));
    }
}
