using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsCompletedElectives.Queries.GetStudentsCompletedElectives;

public sealed record GetStudentsCompletedElectives(string? Period)
    : IRequest<ApiResponse<StudentsCompletedElectivesReportDTO>>;

public sealed class GetStudentsCompletedElectivesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetStudentsCompletedElectives, ApiResponse<StudentsCompletedElectivesReportDTO>>
{
    public async Task<ApiResponse<StudentsCompletedElectivesReportDTO>> Handle(
        GetStudentsCompletedElectives request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_AlumnosCompletaronElectivosAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new StudentCompletedElectiveRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Curso),
            Text(row.Nom_Catedra),
            Text(row.Decreto_Alumno),
            Text(row.TipoCatedra))).ToArray();

        return responses.Success(new StudentsCompletedElectivesReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
