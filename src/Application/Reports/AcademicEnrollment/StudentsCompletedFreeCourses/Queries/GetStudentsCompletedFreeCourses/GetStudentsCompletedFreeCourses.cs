using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsCompletedFreeCourses.Queries.GetStudentsCompletedFreeCourses;

public sealed record GetStudentsCompletedFreeCourses(string? Period)
    : IRequest<ApiResponse<StudentsCompletedFreeCoursesReportDTO>>;

public sealed class GetStudentsCompletedFreeCoursesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetStudentsCompletedFreeCourses, ApiResponse<StudentsCompletedFreeCoursesReportDTO>>
{
    public async Task<ApiResponse<StudentsCompletedFreeCoursesReportDTO>> Handle(
        GetStudentsCompletedFreeCourses request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_AlumnosCompletaronLibresAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new StudentCompletedFreeCourseRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Curso),
            Text(row.Nom_Catedra))).ToArray();

        return responses.Success(new StudentsCompletedFreeCoursesReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
