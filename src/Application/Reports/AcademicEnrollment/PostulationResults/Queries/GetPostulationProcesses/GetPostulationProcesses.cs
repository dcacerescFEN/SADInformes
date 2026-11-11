using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.AcademicEnrollment.PostulationResults.Common;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.PostulationResults.Queries.GetPostulationProcesses;

public sealed record GetPostulationProcesses(string? Origin, string? Period)
    : IRequest<ApiResponse<PostulationProcessesDTO>>;

public sealed class GetPostulationProcessesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPostulationProcesses, ApiResponse<PostulationProcessesDTO>>
{
    public async Task<ApiResponse<PostulationProcessesDTO>> Handle(
        GetPostulationProcesses request, CancellationToken cancellationToken)
    {
        var origin = PostulationRules.Origin(request.Origin);
        var period = PostulationRules.Period(request.Period);
        var data = await procedures.SpRec_ProcesosPeriodosAsync(
            origin, period, 0, cancellationToken: cancellationToken) ?? [];

        var processes = data.Select(row => new PostulationProcessOptionDTO(
            row.IdProcesoPeriodo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.Nombre.Trim(),
            PostulationRules.FormatDate(row.FechaInicio),
            PostulationRules.FormatDate(row.FechaTermino))).ToArray();

        return responses.Success(new PostulationProcessesDTO(processes));
    }
}
