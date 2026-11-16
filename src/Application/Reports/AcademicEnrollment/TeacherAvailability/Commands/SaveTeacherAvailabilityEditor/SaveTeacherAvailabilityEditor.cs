using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.SaveTeacherAvailabilityEditor;

public sealed record SaveTeacherAvailabilityEditor(
    string? Period, string? TeacherCode,
    IReadOnlyList<TeacherAvailabilityEditorCourseUpdateDTO>? Courses,
    IReadOnlyList<TeacherAvailabilityEditorModuleUpdateDTO>? Modules,
    IReadOnlyList<TeacherAvailabilityEditorModuleAddDTO>? NewModules)
    : IRequest<ApiResponse<bool>>;

public sealed class SaveTeacherAvailabilityEditorHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<SaveTeacherAvailabilityEditor, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        SaveTeacherAvailabilityEditor request, CancellationToken cancellationToken)
    {
        var period = request.Period?.Trim() ?? "";
        var teacherCode = request.TeacherCode?.Trim() ?? "";
        if (period.Length is 0 or > 5) throw new ArgumentException("El período no es válido.");
        if (teacherCode.Length is 0 or > 15) throw new ArgumentException("El profesor no es válido.");

        foreach (var course in request.Courses ?? [])
        {
            var code = course.Code?.Trim() ?? "";
            var section = course.Section?.Trim() ?? "";
            if (code.Length is 0 or > 15 || section.Length is 0 or > 15)
                throw new ArgumentException("El curso no es válido.");
            await procedures.SpMOD_FE_TipoModuloCatedrasAsync(
                code, section, course.ModuleType, cancellationToken: cancellationToken);
        }

        foreach (var module in request.Modules ?? [])
        {
            var cascade = module.ModuleCascade?.Trim() ?? "";
            if (module.Id <= 0 || cascade.Length is 0 or > 15)
                throw new ArgumentException("El módulo no es válido.");
            await procedures.SpMOD_FE_ModulosCascadaFinalAsync(
                module.Id, cascade, cancellationToken: cancellationToken);
        }

        foreach (var module in request.NewModules ?? [])
        {
            var cascade = module.ModuleCascade?.Trim() ?? "";
            if (module.ModuleType <= 0 || cascade.Length is 0 or > 15)
                throw new ArgumentException("El módulo nuevo no es válido.");
            await procedures.SpINS_FE_ModulosCascadaFinalAsync(
                period, teacherCode, module.ModuleType, cascade,
                cancellationToken: cancellationToken);
        }

        return responses.Success(true);
    }
}
