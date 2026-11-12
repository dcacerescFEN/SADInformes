using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.TeachingScheduleDecreto.Queries.GetTeachingScheduleDecreto;

public sealed record GetTeachingScheduleDecreto(string? Slug, string? Period)
    : IRequest<ApiResponse<TeachingScheduleDecretoReportDTO>>;

public sealed class GetTeachingScheduleDecretoHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetTeachingScheduleDecreto, ApiResponse<TeachingScheduleDecretoReportDTO>>
{
    public async Task<ApiResponse<TeachingScheduleDecretoReportDTO>> Handle(
        GetTeachingScheduleDecreto request, CancellationToken cancellationToken)
    {
        var definition = TeachingScheduleDecretoCatalog.Resolve(request.Slug);
        var period = TeachingScheduleDecretoRules.Period(request.Period);

        var data = await procedures.SpRec_ProgramacionDocenteXDecretoAsync(
            period, definition.Decree, "PERÍODO", "%", cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new TeachingScheduleDecretoRowDTO(
            Text(row.Curso),
            Text(row.Cod_CatedraHija),
            Text(row.Nom_CatedraHija),
            Text(row.Tipo),
            Number(row.Ud),
            Text(row.Mencion),
            Text(row.Requisitos),
            Text(row.Profesores),
            Text(row.Horario))).ToArray();

        return responses.Success(new TeachingScheduleDecretoReportDTO(
            definition.Title, definition.Slug, period, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";

    private static string Number(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "";
}
