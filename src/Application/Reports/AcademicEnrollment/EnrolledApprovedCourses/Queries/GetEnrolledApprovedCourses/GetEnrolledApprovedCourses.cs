using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.EnrolledApprovedCourses.Queries.GetEnrolledApprovedCourses;

public sealed record GetEnrolledApprovedCourses(string? Period)
    : IRequest<ApiResponse<EnrolledApprovedCoursesReportDTO>>;

public sealed class GetEnrolledApprovedCoursesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetEnrolledApprovedCourses, ApiResponse<EnrolledApprovedCoursesReportDTO>>
{
    public async Task<ApiResponse<EnrolledApprovedCoursesReportDTO>> Handle(
        GetEnrolledApprovedCourses request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_InscritasYaAprobadasAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new EnrolledApprovedCourseRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Cod_Catedra),
            Text(row.Nom_Catedra),
            Text(row.Cod_Seccion),
            Text(row.Origen))).ToArray();

        return responses.Success(new EnrolledApprovedCoursesReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
