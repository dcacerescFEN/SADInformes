using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentFilters;

public sealed record GetMassEnrollmentFilters(MassEnrollmentSelection Selection, string User)
    : IRequest<ApiResponse<MassEnrollmentFiltersDTO>>;

public sealed class GetMassEnrollmentFiltersHandler(ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetMassEnrollmentFilters, ApiResponse<MassEnrollmentFiltersDTO>>
{
    public async Task<ApiResponse<MassEnrollmentFiltersDTO>> Handle(GetMassEnrollmentFilters request, CancellationToken ct)
    {
        MassEnrollmentRules.ValidateIdentity(request.User);
        MassEnrollmentRules.Validate(request.Selection, required: false);
        return responses.Success(await ReadAsync(procedures, request.Selection, request.User, ct));
    }

    public static async Task<MassEnrollmentFiltersDTO> ReadAsync(ISadContextProcedures procedures, MassEnrollmentSelection selection, string user, CancellationToken ct)
    {
        var types = await procedures.SpRec_TiposAlumnoAsync("NO", user, cancellationToken: ct) ?? [];
        var studentTypes = types.Where(row => Concrete(row.Tipo))
            .Select(row => new MassEnrollmentOptionDTO(row.Tipo.Trim(), row.Nombre?.Trim() ?? row.Tipo)).ToArray();
        var years = await procedures.SpRec_AnosAsync("NO", "Todos", cancellationToken: ct) ?? [];
        var cohorts = years.Select(row => row.Año?.Trim()).Where(Concrete).Cast<string>().Distinct().ToArray();
        var type = Default(selection.StudentType, studentTypes);
        if (!studentTypes.Any(option => option.Value == type))
            return new(studentTypes, [], cohorts, [], []);

        var modes = await procedures.SpRec_TiposAlumnosModalidadesAsync(type, cancellationToken: ct) ?? [];
        var modalities = modes.Select(row => row.Modalidad?.Trim()).Where(Concrete).Cast<string>().Distinct().ToArray();
        var periods = (await procedures.SpRec_PeriodosAcademicosFiltradosAsync("Tipo_Alumno", type, "DESCENDENTE", cancellationToken: ct) ?? [])
            .Where(row => Concrete(row.Periodo))
            .Select(row => new MassEnrollmentOptionDTO(row.Periodo!.Trim(), row.PeriodoLargo?.Trim() ?? row.Periodo)).ToArray();
        var modality = string.IsNullOrEmpty(selection.Modality) ? modalities.FirstOrDefault() ?? "" : selection.Modality;
        var period = Default(selection.Period, periods);
        var courses = !modalities.Contains(modality) || !periods.Any(option => option.Value == period)
            ? []
            : (await procedures.SpRec_CursosxPeriodoAsync(period, type, modality, cancellationToken: ct) ?? [])
                .Where(row => Concrete(row.Cod_Catedra))
                .Select(row => new MassEnrollmentOptionDTO(row.Cod_Catedra.Trim(), row.NombreCatedra?.Trim() ?? row.Cod_Catedra))
                .DistinctBy(row => row.Value).ToArray();
        return new(studentTypes, modalities, cohorts, periods, courses);
    }

    private static string Default(string value, MassEnrollmentOptionDTO[] options) =>
        string.IsNullOrEmpty(value) ? options.FirstOrDefault()?.Value ?? "" : value;
    private static bool Concrete(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Todos", StringComparison.OrdinalIgnoreCase) && value != "%";
}
