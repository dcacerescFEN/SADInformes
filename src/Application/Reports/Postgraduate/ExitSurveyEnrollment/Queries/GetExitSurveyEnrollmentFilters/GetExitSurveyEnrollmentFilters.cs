using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollmentFilters;

public sealed record GetExitSurveyEnrollmentFilters(string? StudentType) : IRequest<ApiResponse<ExitSurveyEnrollmentFiltersDTO>>;
public sealed class GetExitSurveyEnrollmentFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses, IPostgraduateSettings settings)
    : IRequestHandler<GetExitSurveyEnrollmentFilters, ApiResponse<ExitSurveyEnrollmentFiltersDTO>>
{
    public async Task<ApiResponse<ExitSurveyEnrollmentFiltersDTO>> Handle(GetExitSurveyEnrollmentFilters request, CancellationToken ct) =>
        responses.Success(await ReadAsync(procedures, settings, request.StudentType, ct));

    internal static async Task<ExitSurveyEnrollmentFiltersDTO> ReadAsync(ISadContextProcedures procedures, IPostgraduateSettings settings, string? requested, CancellationToken ct)
    {
        var types = (await procedures.spRec_EncuestaGraduados_TiposAlumnoAsync("SI", settings.User, cancellationToken: ct) ?? [])
            .Where(row => !string.IsNullOrWhiteSpace(row.Tipo))
            .Select(row => new ExitSurveyOptionDTO(row.Tipo!.Trim(), row.Nombre?.Trim() ?? row.Tipo.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        if (types.Length == 0) throw new ArgumentException("No hay tipos de alumno disponibles.");
        var selected = string.IsNullOrWhiteSpace(requested) ? types[0].Value : requested.Trim();
        if (!types.Any(row => row.Value == selected)) throw new ArgumentException("El tipo de alumno no es válido.");
        var modalities = (await procedures.spRec_EncuestaGraduados_ModalidadesAsync(selected, cancellationToken: ct) ?? [])
            .Where(row => !string.IsNullOrWhiteSpace(row.Modalidad))
            .Select(row => new ExitSurveyOptionDTO(row.Modalidad!.Trim(), row.Modalidad.Trim()))
            .DistinctBy(row => row.Value).ToArray();
        var years = (await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [])
            .Select(row => row.Año?.Trim()).Where(value => ExitSurveyRules.YearOrNull(value) is not null)
            .Cast<string>().Distinct(StringComparer.Ordinal).ToArray();
        return new(types, modalities, years);
    }
}
