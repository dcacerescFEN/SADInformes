using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PermutationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.RoomAssignmentCourses.Queries.GetRoomAssignmentCourses;

public sealed record GetRoomAssignmentCourses(string? Period)
    : IRequest<ApiResponse<RoomAssignmentCoursesReportDTO>>;

public sealed class GetRoomAssignmentCoursesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetRoomAssignmentCourses, ApiResponse<RoomAssignmentCoursesReportDTO>>
{
    public async Task<ApiResponse<RoomAssignmentCoursesReportDTO>> Handle(
        GetRoomAssignmentCourses request, CancellationToken cancellationToken)
    {
        var period = PermutationRules.Period(request.Period);
        var data = await procedures.SpRec_CursosSistemaAsignacionSalasAsync(
            period, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new RoomAssignmentCourseRowDTO(
            Text(row.Periodo),
            Text(row.Cod_Catedra),
            Text(row.Cod_Seccion),
            Text(row.Eliminado),
            Text(row.Clases),
            Text(row.Tipo),
            Number(row.VacantesOfrecidas),
            Number(row.VacantesOcupadas),
            Text(row.Profesor1),
            Text(row.Profesor2),
            Text(row.Profesor3),
            Text(row.Profesor4),
            Text(row.Profesor5),
            Text(row.Profesor6),
            Text(row.Profesor7),
            Text(row.Profesor8),
            Text(row.Profesor9),
            Text(row.Profesor10))).ToArray();

        return responses.Success(new RoomAssignmentCoursesReportDTO(period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";

    private static string Number(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "";
}
