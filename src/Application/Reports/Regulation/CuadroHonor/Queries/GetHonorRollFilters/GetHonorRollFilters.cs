using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Regulation.CuadroHonor.Queries.GetHonorRollFilters;

public sealed record GetHonorRollFilters : IRequest<ApiResponse<HonorRollFiltersDTO>>;

public sealed class GetHonorRollFiltersHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetHonorRollFilters, ApiResponse<HonorRollFiltersDTO>>
{
    // Legacy call spRec_Años 'NO', 'Todos': a null @TipoAlumno makes the SP return no rows.
    private static readonly HonorRollCareerDTO[] Careers =
    [
        new("IC", "Ingeniería Comercial"),
        new("IICG", "Ingeniería en Información y Control de Gestión y Auditoría"),
        new("CA", "Contador Auditor"),
    ];

    public async Task<ApiResponse<HonorRollFiltersDTO>> Handle(
        GetHonorRollFilters request, CancellationToken cancellationToken)
    {
        var data = await procedures.SpRec_AnosAsync(
            "NO", "Todos", cancellationToken: cancellationToken) ?? [];

        var years = data
            .Select(row => row.Año?.Trim() ?? "")
            .Where(year => year.Length == 4 && year.All(char.IsAsciiDigit))
            .Distinct(StringComparer.Ordinal)
            .Select(year => new HonorRollYearDTO(year, year))
            .ToArray();

        return responses.Success(new HonorRollFiltersDTO(Careers, years));
    }
}
