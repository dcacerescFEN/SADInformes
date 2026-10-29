using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.StudentRequests.Queries.GetStudentRequestsFilters;

public sealed record GetStudentRequestsFilters(string? StudentType) : IRequest<ApiResponse<StudentRequestsFiltersDTO>>;

public sealed class GetStudentRequestsFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetStudentRequestsFilters, ApiResponse<StudentRequestsFiltersDTO>>
{
    public async Task<ApiResponse<StudentRequestsFiltersDTO>> Handle(GetStudentRequestsFilters request, CancellationToken ct) =>
        responses.Success(await ReadAsync(procedures, request.StudentType, ct));

    internal static readonly string[] States = ["Todos", "PENDIENTE", "EN CURSO", "RESUELTA", "ANULADA"];

    internal static async Task<StudentRequestsFiltersDTO> ReadAsync(ISadContextProcedures procedures, string? studentType, CancellationToken ct)
    {
        var type = Normalize(studentType);
        if (type.Length > 30 || type.Any(char.IsControl)) throw new ArgumentException("El tipo de alumno no es válido.");
        // Same broad catalog as the legacy page; the report-specific permission protects all routes.
        var types = await procedures.SpRec_TiposAlumnoAsync("SI", "", cancellationToken: ct) ?? [];
        var options = types.Where(row => !string.IsNullOrWhiteSpace(row.Tipo))
            .Select(row => new StudentRequestsOptionDTO(row.Tipo.Trim(), row.Nombre?.Trim() ?? row.Tipo.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        if (type.Length > 0 && !options.Any(row => row.Value == type))
            throw new ArgumentException("El tipo de alumno no es válido.");
        var years = await procedures.SpRec_AnosAsync("SI", "Todos", cancellationToken: ct) ?? [];
        var modes = await procedures.SpRec_TiposAlumnosModalidadesAsync(type.Length == 0 ? "Todos" : type, cancellationToken: ct) ?? [];
        return new(
            [new("Todos", "Todos"), .. options.Where(row => row.Value != "Todos")],
            modes.Select(row => row.Modalidad?.Trim()).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().Distinct().ToArray(),
            ["Todos", .. years.Select(row => row.Año?.Trim()).Where(value => !string.IsNullOrWhiteSpace(value) && value != "Todos").Cast<string>().Distinct()],
            States.ToArray());
    }

    internal static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) || value.Trim() == "Todos" ? "" : value.Trim();
}
