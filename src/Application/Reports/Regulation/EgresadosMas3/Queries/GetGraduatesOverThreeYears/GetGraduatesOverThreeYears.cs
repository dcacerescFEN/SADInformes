using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Regulation.EgresadosMas3.Queries.GetGraduatesOverThreeYears;

public sealed record GetGraduatesOverThreeYears
    : IRequest<ApiResponse<GraduatesOverThreeYearsReportDTO>>;

public sealed class GetGraduatesOverThreeYearsHandler(
    ISadContext sad, ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetGraduatesOverThreeYears, ApiResponse<GraduatesOverThreeYearsReportDTO>>
{
    public async Task<ApiResponse<GraduatesOverThreeYearsReportDTO>> Handle(
        GetGraduatesOverThreeYears request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;
        var period = await sad.PeriodosAcademicos.AsNoTracking()
            .Where(item => item.Tipo == "SEMESTRAL"
                && item.InicioPeriodo.HasValue && item.FinPeriodo.HasValue
                && item.InicioPeriodo.Value.Date <= today
                && item.FinPeriodo.Value.Date >= today)
            .Select(item => item.Periodo)
            .FirstOrDefaultAsync(cancellationToken) ?? "";
        if (period.Length == 0)
            throw new ArgumentException("No hay un período académico activo.");

        var data = await procedures.SpRec_AlumnosEgresadosMasTresAnosAsync(
            period, cancellationToken: cancellationToken) ?? [];
        var rows = new List<GraduatesOverThreeYearsRowDTO>(data.Count);
        foreach (var row in data)
        {
            rows.Add(new GraduatesOverThreeYearsRowDTO(
                Text(row.Cod_Alumno), Text(row.Nombre), Text(row.Tipo_Alumno),
                Text(row.Decreto), Text(row.Decreto2), Text(row.Mencion),
                Text(row.EgresadoDe), Text(row.Situacion), Text(row.Sem_Ingreso),
                Text(row.Promocion)));
        }

        return responses.Success(new GraduatesOverThreeYearsReportDTO(period, rows));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
