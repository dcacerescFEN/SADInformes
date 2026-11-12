using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.EnrollmentTeachingSchedule.Queries.GetEnrollmentTeachingSchedule;

public sealed record GetEnrollmentTeachingSchedule(string? School, string? Period, string? Area)
    : IRequest<ApiResponse<EnrollmentTeachingScheduleReportDTO>>;

public sealed class GetEnrollmentTeachingScheduleHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetEnrollmentTeachingSchedule, ApiResponse<EnrollmentTeachingScheduleReportDTO>>
{
    public async Task<ApiResponse<EnrollmentTeachingScheduleReportDTO>> Handle(
        GetEnrollmentTeachingSchedule request, CancellationToken cancellationToken)
    {
        var school = EnrollmentTeachingScheduleRules.School(request.School);
        var period = EnrollmentTeachingScheduleRules.Period(request.Period);
        var area = EnrollmentTeachingScheduleRules.Area(request.Area);
        var coordination = EnrollmentTeachingScheduleRules.Coordination(school);

        var data = await procedures.SpRec_ProgramacionDocenteResumidaAsync(
            period, coordination, area, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new EnrollmentTeachingScheduleRowDTO(
            Text(row.Curso),
            Text(row.Nom_Catedra),
            Text(row.Profesor),
            Text(row.Clases_Catedra),
            Text(row.Clases_Ayudantia),
            Text(row.Salas_Catedra),
            Text(row.Salas_Ayudantia),
            Number(row.Vacantes),
            Number(row.Inscritos),
            Text(row.Area),
            Text(row.RutProfesor))).ToArray();

        return responses.Success(new EnrollmentTeachingScheduleReportDTO(
            period, school, area, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";

    private static string Number(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "";
}
