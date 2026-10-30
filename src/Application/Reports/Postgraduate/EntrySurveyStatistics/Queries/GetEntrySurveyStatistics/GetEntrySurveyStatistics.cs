using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.EntrySurveyStatistics.Queries.GetEntrySurveyStatistics;

public sealed record GetEntrySurveyStatistics : IRequest<ApiResponse<EntrySurveyReportDTO>>;

public sealed class GetEntrySurveyStatisticsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetEntrySurveyStatistics, ApiResponse<EntrySurveyReportDTO>>
{
    public async Task<ApiResponse<EntrySurveyReportDTO>> Handle(
        GetEntrySurveyStatistics request, CancellationToken ct)
    {
        var data = await procedures.SpRec_EstadisticaEncuestaEntradaAsync(cancellationToken: ct) ?? [];
        var rows = data.Select(row => new EntrySurveyDTO(
            Text(row.Cod_Alumno), Text(row.Tipo_Alumno), Text(row.Modalidad), Text(row.Q1),
            Text(row.Q2_1), Text(row.Q2_2), Text(row.Q2_3), Text(row.Q2_4), Text(row.Q2_5),
            Text(row.Q2_6), Text(row.Q2_7), Text(row.Q2_8), Text(row.Q3_1), Text(row.Q3_2),
            Text(row.Q3_3), Text(row.Q3_4), Text(row.Q3_5), Text(row.Q3_6), Text(row.Q3_7),
            Text(row.Q4_1), Text(row.Q4_2), Text(row.Q4_3), Text(row.Q4_4), Text(row.Q4_5),
            Text(row.Q4_6), Text(row.Q4_7), Text(row.Q4_8), Text(row.Q5), Text(row.Q5_otro),
            Text(row.Q6), Text(row.Q7), Text(row.Q8), Text(row.Q9), Text(row.Q9_otro),
            Text(row.Q10), Text(row.Q11), Text(row.Q11_otro), Text(row.Q12), Text(row.Q13),
            Text(row.Q14), Text(row.Q15), Text(row.Q16), Text(row.Q17), Date(row.Fecha))).ToArray();
        return responses.Success(new EntrySurveyReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Date(DateTime? value) =>
        value?.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}
