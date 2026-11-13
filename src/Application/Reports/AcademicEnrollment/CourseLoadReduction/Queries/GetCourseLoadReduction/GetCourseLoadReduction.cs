using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.CourseLoadReduction.Queries.GetCourseLoadReduction;

public sealed record GetCourseLoadReduction(string? Period)
    : IRequest<ApiResponse<CourseLoadReductionReportDTO>>;

public sealed class GetCourseLoadReductionHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetCourseLoadReduction, ApiResponse<CourseLoadReductionReportDTO>>
{
    public async Task<ApiResponse<CourseLoadReductionReportDTO>> Handle(
        GetCourseLoadReduction request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_ReduccionCargasAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new CourseLoadReductionRowDTO(
            Text(row.Periodo),
            Text(row.Cod_Catedra),
            Text(row.Cod_Seccion),
            Text(row.Nom_Catedra),
            Text(row.Cod_Alumno),
            Text(row.Cod_Persona),
            Text(row.Decreto),
            Text(row.Nombre),
            Text(row.Origen),
            Text(row.TipoMovimiento),
            Text(row.Estado))).ToArray();

        return responses.Success(new CourseLoadReductionReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
