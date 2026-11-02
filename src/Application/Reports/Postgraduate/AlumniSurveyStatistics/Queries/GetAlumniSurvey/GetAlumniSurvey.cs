using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.AlumniSurveyStatistics.Queries.GetAlumniSurvey;

public sealed record GetAlumniSurvey : IRequest<ApiResponse<AlumniSurveyReportDTO>>;

public sealed class GetAlumniSurveyHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetAlumniSurvey, ApiResponse<AlumniSurveyReportDTO>>
{
    public async Task<ApiResponse<AlumniSurveyReportDTO>> Handle(GetAlumniSurvey request, CancellationToken ct)
    {
        var data = await procedures.SpRec_EstadisticaEncuestaAlumniAsync(cancellationToken: ct) ?? [];
        var rows = data.Select(row => new AlumniSurveyDTO(
            Text(row.Cod_Alumno), Text(row.P1), Text(row.P1_1), Text(row.P1_2), Text(row.P1_3),
            Text(row.P1_4), Text(row.P2), Text(row.P2_1), Text(row.P2_2), Text(row.P2_3),
            Text(row.P2_4), Text(row.P3_1), Text(row.P3_2), Text(row.P3_3), Text(row.P3_4),
            Text(row.P4), Text(row.P5), Text(row.P6), Text(row.P7), Text(row.P8_1), Text(row.P8_2),
            Text(row.P9), Number(row.P10), Text(row.P10_1), Text(row.P10_2), Text(row.P10_3),
            Text(row.P10_4), Text(row.P11), Text(row.P12), Text(row.P13), Text(row.P14),
            Date(row.Fecha))).ToArray();
        return responses.Success(new AlumniSurveyReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Date(DateTime? value) =>
        value?.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}
