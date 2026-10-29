using System.Data;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Common;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollmentFilters;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.ExitSurveyEnrollment.Commands.CreateExitSurveyEnrollment;

public sealed record CreateExitSurveyEnrollment(CreateExitSurveyEnrollmentRequestDTO Request)
    : IRequest<ApiResponse<CreateExitSurveyEnrollmentResultDTO>>;

public sealed class CreateExitSurveyEnrollmentHandler(
    ISadContext context, ISadContextProcedures procedures, IApiResponseService responses, IPostgraduateSettings settings)
    : IRequestHandler<CreateExitSurveyEnrollment, ApiResponse<CreateExitSurveyEnrollmentResultDTO>>
{
    public async Task<ApiResponse<CreateExitSurveyEnrollmentResultDTO>> Handle(CreateExitSurveyEnrollment request, CancellationToken ct)
    {
        var enrollment = request.Request;
        if (!enrollment.Confirmed) throw new ArgumentException("Debe confirmar la inscripción.");
        var type = ExitSurveyRules.Text(enrollment.StudentType, "El tipo de alumno no es válido.");
        var modality = ExitSurveyRules.Text(enrollment.Modality, "La modalidad no es válida.");
        var year = ExitSurveyRules.Year(enrollment.Year);
        var filters = await GetExitSurveyEnrollmentFiltersHandler.ReadAsync(procedures, settings, type, ct);
        if (!filters.StudentTypes.Any(row => row.Value == type)) throw new ArgumentException("El tipo de alumno no es válido.");
        if (!filters.Modalities.Any(row => row.Value == modality)) throw new ArgumentException("La modalidad no es válida.");
        if (!filters.Years.Contains(year, StringComparer.Ordinal)) throw new ArgumentException("El año no es válido.");
        var codes = enrollment.StudentCodes;
        if (codes is null || codes.Length == 0 || codes.Any(code => string.IsNullOrWhiteSpace(code)
                || code.Length > 20 || code.Any(char.IsControl))
            || codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Length)
            throw new ArgumentException("Seleccione alumnos válidos sin duplicados.");
        var periods = codes.ToDictionary(code => code, ExitSurveyRules.PeriodFromCode, StringComparer.OrdinalIgnoreCase);

        await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var rows = await ExitSurveyRules.ReadListAsync(procedures, type, modality, year, ct);
        var selected = new List<ExitSurveyStudent>();
        foreach (var code in codes)
        {
            var row = rows.FirstOrDefault(candidate => string.Equals(candidate.StudentCode, code, StringComparison.OrdinalIgnoreCase));
            if (row is null) throw new ExitSurveyConflictException("La selección cambió o incluye alumnos ya inscritos. Actualice el listado.");
            selected.Add(row);
        }
        var existing = await context.EncuestaSalidaGraduados.AsNoTracking()
            .Where(row => codes.Contains(row.CodAlumno)).Select(row => row.CodAlumno).ToArrayAsync(ct);
        var newCodes = selected.Where(row => !existing.Contains(row.StudentCode, StringComparer.OrdinalIgnoreCase)).ToArray();
        foreach (var row in newCodes)
        {
            await procedures.spCre_EncuestaGraduados_Ins_AlumnoAsync(
                row.StudentCode, periods[row.StudentCode], ExitSurveyRules.SpDate(row.ExitDate), cancellationToken: ct);
        }
        // The SP has no reliable affected-row signal; verify every new row before committing.
        var missing = await context.EncuestaSalidaGraduados.AsNoTracking()
            .Where(row => newCodes.Select(value => value.StudentCode).Contains(row.CodAlumno)).Select(row => row.CodAlumno).ToArrayAsync(ct);
        if (newCodes.Any(row => !missing.Contains(row.StudentCode, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Survey enrollment postcondition failed.");
        await transaction.CommitAsync(ct);
        return responses.Success(new CreateExitSurveyEnrollmentResultDTO(newCodes.Length, selected.Count - newCodes.Length));
    }
}
