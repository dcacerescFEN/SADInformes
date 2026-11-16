using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingFilters;

public sealed record GetAutomaticProcessingFilters
    : IRequest<ApiResponse<AutomaticProcessingRequestsFiltersDTO>>;

public sealed class GetAutomaticProcessingFiltersHandler(
    ISadContext sad, IApiResponseService responses)
    : IRequestHandler<GetAutomaticProcessingFilters, ApiResponse<AutomaticProcessingRequestsFiltersDTO>>
{
    private static readonly string[] AutomaticTypes = ["2", "8", "10"];

    public async Task<ApiResponse<AutomaticProcessingRequestsFiltersDTO>> Handle(
        GetAutomaticProcessingFilters request, CancellationToken cancellationToken)
    {
        var periods = await sad.Solicitudes89.AsNoTracking()
            .Where(row => row.PeriodoSolicitud != null && AutomaticTypes.Contains(row.TipoSolicitud!))
            .Select(row => row.PeriodoSolicitud!)
            .Distinct()
            .OrderByDescending(period => period)
            .ToListAsync(cancellationToken);

        var states = await sad.Solicitudes89.AsNoTracking()
            .Where(row => row.TramitacionAutomatizada != null
                && row.TramitacionAutomatizada.Trim() != "")
            .Select(row => row.TramitacionAutomatizada!.Trim().ToUpper())
            .Distinct()
            .OrderBy(state => state)
            .ToListAsync(cancellationToken);
        var stateOptions = new List<RequestOptionDTO>
        {
            new("", "TODAS"),
            new("SIN DATO", "SIN DATO"),
        };
        stateOptions.AddRange(states.Select(state => new RequestOptionDTO(state, state)));

        return responses.Success(new AutomaticProcessingRequestsFiltersDTO(
            periods,
            [
                new("", "TODAS"), new("IC", "Ingeniería Comercial"),
                new("IICG", "Ingeniería en Información y Control de Gestión"),
                new("CA", "Contador Auditor"),
            ],
            [
                new("", "TODOS"),
                new("DIFERIR LA INSCRIPCION DE ASIGNATURAS REPROBADAS", "Diferir la inscripción de asignaturas reprobadas"),
                new("INSCRIBIR CARGA SUPERIOR A LA MAXIMA", "Inscribir carga superior a la máxima"),
                new("INSCRIBIR JUNTO CON REQUISITOS", "Inscribir junto con requisitos"),
                new("EXTENSIÓN DE PLAZO OBTENCIÓN TITULO", "Extensión de plazo obtención título"),
            ],
            stateOptions));
    }
}
