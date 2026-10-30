using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.ThesisRegistration.Queries.GetThesisRegistration;

public sealed record GetThesisRegistration(string? StudentType, string? Modality, string? Year)
    : IRequest<ApiResponse<ThesisRegistrationReportDTO>>;

public sealed class GetThesisRegistrationHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetThesisRegistration, ApiResponse<ThesisRegistrationReportDTO>>
{
    public async Task<ApiResponse<ThesisRegistrationReportDTO>> Handle(
        GetThesisRegistration request, CancellationToken ct)
    {
        var type = ThesisRegistrationCatalog.Value(request.StudentType);
        var modality = ThesisRegistrationCatalog.Value(request.Modality);
        var year = ThesisRegistrationCatalog.Value(request.Year);
        var filters = await ThesisRegistrationCatalog.ReadAsync(procedures, type, ct);
        if (!filters.StudentTypes.Any(row => row.Value == type)
            || !filters.Modalities.Contains(modality) || !filters.Years.Contains(year))
            throw new ArgumentException("Seleccione filtros válidos del catálogo.");
        var data = await procedures.SpRec_RegistroTesisPostgradoAsync(
            type, modality, year, cancellationToken: ct) ?? [];
        var rows = data.Select(row => new ThesisRegistrationDTO(
            Text(row.Cod_Alumno), Text(row.Matricula), Text(row.Sem_IngresoDecreto), Text(row.Decreto),
            Text(row.Cod_Persona), Text(row.Tipo_Alumno), Text(row.Modalidad), Text(row.Apellidos),
            Text(row.Nombres), Text(row.CatedraAFE), Text(row.PeriodoAFE), Text(row.ProfesorAFE),
            Text(row.NotaAFE), Text(row.Cod_Catedra), Text(row.Periodo), Text(row.Cod_SitAcademica),
            Text(row.Motivo), Text(row.Nombre), Number(row.Nota), Text(row.Profesores),
            Date(row.Fecha_Examen))).ToArray();
        return responses.Success(new ThesisRegistrationReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Date(DateTime? value) => value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
}
