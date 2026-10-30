using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalData;

public sealed record GetPersonalData(string? StudentType, string? Modality, string? Year)
    : IRequest<ApiResponse<PersonalDataReportDTO>>;

public sealed class GetPersonalDataHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetPersonalData, ApiResponse<PersonalDataReportDTO>>
{
    public async Task<ApiResponse<PersonalDataReportDTO>> Handle(GetPersonalData request, CancellationToken ct)
    {
        var type = PersonalDataCatalog.Value(request.StudentType, "Seleccione un tipo de alumno.");
        var modality = PersonalDataCatalog.Value(request.Modality, "Seleccione una modalidad.");
        var year = PersonalDataCatalog.Value(request.Year, "Seleccione un año de ingreso.");
        var filters = await PersonalDataCatalog.ReadAsync(procedures, type, ct);
        if (!filters.StudentTypes.Any(row => row.Value == type)
            || !filters.Modalities.Contains(modality) || !filters.Years.Contains(year))
            throw new ArgumentException("Seleccione filtros válidos del catálogo.");
        var data = await procedures.SpRec_AlumnosDatosPersonalesPostgradoAsync(
            type, modality, year, cancellationToken: ct) ?? [];
        var rows = data.Select(row => new PersonalDataDTO(
            row.ID_Alumno.ToString(CultureInfo.InvariantCulture), Text(row.Cod_Alumno),
            Text(row.Sem_IngresoDecreto), Text(row.Cod_Persona), Text(row.Tipo_Alumno),
            Text(row.Modalidad), Text(row.Cod_SitAcademica), Text(row.Motivo), Text(row.Nombres),
            Text(row.Apellidos), Text(row.Email_Personal), Text(row.Email_Fen), Text(row.Email_laboral),
            Text(row.Celular), Text(row.Tel_laboral), Text(row.NomEmpresa), Text(row.CargoEmpresa),
            Date(row.Fec_Nacimiento), Text(row.Nacionalidad), Text(row.Sexo), Text(row.CarreraAnterior),
            Text(row.Universidad_de_Procedencia), Text(row.FechaResolucion), Text(row.Fecha_Examen),
            Text(row.Ultimo_Periodo_Inscripcion_Curso), Text(row.Periodo_Examen))).ToArray();
        return responses.Success(new PersonalDataReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Date(DateTime? value) =>
        value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
}
