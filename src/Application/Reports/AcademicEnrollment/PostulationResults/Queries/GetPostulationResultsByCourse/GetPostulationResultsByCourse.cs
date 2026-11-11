using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PostulationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationResultsByCourse;

public sealed record GetPostulationResultsByCourse(string? Period, string? Origin, string? From, string? To)
    : IRequest<ApiResponse<PostulationResultsReportDTO>>;

public sealed class GetPostulationResultsByCourseHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPostulationResultsByCourse, ApiResponse<PostulationResultsReportDTO>>
{
    public async Task<ApiResponse<PostulationResultsReportDTO>> Handle(
        GetPostulationResultsByCourse request, CancellationToken cancellationToken)
    {
        var period = PostulationRules.Period(request.Period);
        var origin = PostulationRules.Origin(request.Origin);
        var from = PostulationRules.Date(request.From, "desde");
        var to = PostulationRules.Date(request.To, "hasta");
        if (from > to) throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.");

        var data = await procedures.SpRec_ResPostulaAgregaXCursoAsync(
            period, origin, from, to, cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new PostulationResultRowDTO(
            Text(row.Curso),
            Text(row.Nom_Catedra),
            Text(row.Tipo_Alumno),
            Number(row.VacOrig),
            Number(row.AcepOtros),
            Number(row.VacProc),
            Number(row.MovProc),
            Number(row.AcepProc),
            Number(row.AcepProc),
            Number(row.AcepTot),
            Number(row.VacDisp),
            Number(row.RecVac),
            Number(row.RecVacReal),
            Number(row.RecTope),
            Number(row.RecCarga),
            Number(row.RecReq),
            Number(row.RecOtros))).ToArray();

        return responses.Success(new PostulationResultsReportDTO(period, origin, rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static int Number(short? value) => value ?? 0;
    private static int Number(int? value) => value ?? 0;
}
