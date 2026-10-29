using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Common.Reporting;
using Application.Reports.Postgraduate.StudentRequests.Queries.GetStudentRequestsFilters;

namespace Application.Reports.Postgraduate.StudentRequests.Queries.GetStudentRequests;

public sealed record GetStudentRequests(string? StudentType, string? Modality, string? Cohort, string? State, string? Format)
    : IRequest<IReportRenderResult>;

public sealed class GetStudentRequestsHandler(ISadContextProcedures procedures, IReportRenderer renderer)
    : IRequestHandler<GetStudentRequests, IReportRenderResult>
{
    public async Task<IReportRenderResult> Handle(GetStudentRequests request, CancellationToken ct)
    {
        // Browser input never determines the report path, name, endpoint or parameter names.
        if (request.Format is not null and not "EXCEL")
            throw new ArgumentException("El formato no es válido. Solo se permite EXCEL.");
        var state = GetStudentRequestsFiltersHandler.Normalize(request.State);
        if (state.Length > 0 && !GetStudentRequestsFiltersHandler.States.Contains(state))
            throw new ArgumentException("El estado no es válido.");
        var filters = await GetStudentRequestsFiltersHandler.ReadAsync(procedures, request.StudentType, ct);
        var type = GetStudentRequestsFiltersHandler.Normalize(request.StudentType);
        var modality = request.Modality?.Trim() ?? "";
        var cohort = GetStudentRequestsFiltersHandler.Normalize(request.Cohort);
        if (modality.Length > 0 && !filters.Modalities.Contains(modality))
            throw new ArgumentException("La modalidad no es válida.");
        if (cohort.Length > 0 && !filters.Cohorts.Contains(cohort))
            throw new ArgumentException("El año de ingreso no es válido.");
        var parameters = new Dictionary<string, string>();
        if (type.Length > 0) parameters["TipoAlumno"] = type;
        if (modality.Length > 0) parameters["Modalidad"] = modality;
        if (cohort.Length > 0) parameters["AñoDeIngreso"] = cohort;
        if (state.Length > 0) parameters["EstadoSolicitud"] = state;
        return await renderer.RenderAsync(new ReportRenderRequest("SolicitudesAlumnoPostgrado",
            parameters, ReportFormat.EXCEL, TimeSpan.FromSeconds(60)), ct);
    }
}
