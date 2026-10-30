using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefenses;

public sealed record GetThesisDefenses(
    string? DefenseYear, string? StudentType, string? Modality, string? EntryYear)
    : IRequest<ApiResponse<ThesisDefensesReportDTO>>;

public sealed class GetThesisDefensesHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetThesisDefenses, ApiResponse<ThesisDefensesReportDTO>>
{
    public async Task<ApiResponse<ThesisDefensesReportDTO>> Handle(
        GetThesisDefenses request, CancellationToken ct)
    {
        var (defenseYear, studentType, modality, entryYear) =
            await ThesisDefensesCatalog.ValidateAsync(
                procedures, request.DefenseYear, request.StudentType, request.Modality,
                request.EntryYear, ct);
        var coordination = await procedures.SpRec_InformePostgradoProfesoresGCIAsync(
            defenseYear, studentType, modality, entryYear, cancellationToken: ct) ?? [];
        var treasury = await procedures.SpRec_InformePostgradoProfesoresGCI2Async(
            defenseYear, studentType, modality, entryYear, cancellationToken: ct) ?? [];
        var coordinationRows = coordination.Select(row => new CoordinationDefenseDTO(
            Text(row.Cod_Alumno), Text(row.Nombre), Text(row.Programa), Text(row.Modalidad),
            Text(row.AñoIngreso), Text(row.FechaExamen), Text(row.Periodo), Text(row.Cod_Catedra),
            Text(row.Cod_Seccion), Text(row.Nota), Text(row.Estado_Examen),
            Text(row.TemaNombreTesis), Text(row.GUIA), Text(row.COGUIA), Text(row.INFORMANTE1),
            Text(row.INFORMANTE2), Text(row.INFORMANTE3), Text(row.CentroCosto),
            Text(row.AñoDefensa), Text(row.FechaResolucion))).ToArray();
        var treasuryRows = treasury.Select(row => new TreasuryDefenseDTO(
            Text(row.Periodo), Text(row.Cod_Catedra), Text(row.Cod_Seccion), Text(row.Cod_Profesor),
            Text(row.NombreProfesor), Text(row.Tipo), Text(row.Cod_Alumno), Text(row.Nota),
            Text(row.Estudiante), Text(row.Programa), Text(row.Modalidad), Text(row.AñoIngreso),
            Text(row.TemaNombreTesis), Date(row.Fecha_Examen), Text(row.Estado_Examen),
            Text(row.CentroCosto), Text(row.FechaResolucion))).ToArray();
        return responses.Success(new ThesisDefensesReportDTO(
            coordinationRows, treasuryRows, coordinationRows.Length, treasuryRows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Date(DateTime? value) =>
        value?.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}
