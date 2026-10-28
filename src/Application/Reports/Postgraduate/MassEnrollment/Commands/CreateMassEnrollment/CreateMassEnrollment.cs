using System.Data;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Commands.CreateMassEnrollment;

public sealed record CreateMassEnrollment(MassEnrollmentRequestDTO Enrollment, string User, string Pc)
    : IRequest<ApiResponse<MassEnrollmentResultDTO>>;

public sealed class CreateMassEnrollmentHandler(ISadContext context, ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<CreateMassEnrollment, ApiResponse<MassEnrollmentResultDTO>>
{
    public async Task<ApiResponse<MassEnrollmentResultDTO>> Handle(CreateMassEnrollment request, CancellationToken ct)
    {
        var user = request.User;
        var pc = request.Pc;
        MassEnrollmentRules.ValidateIdentity(user);
        var selection = request.Enrollment.Selection ?? throw new ArgumentException("Seleccione los filtros de inscripción.");
        MassEnrollmentRules.Validate(selection, required: true);
        if (!request.Enrollment.Confirmed) throw new ArgumentException("Debe confirmar la inscripción.");
        var students = request.Enrollment.StudentCodes;
        if (students is null || students.Length == 0 || students.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 20)
            || students.Distinct(StringComparer.Ordinal).Count() != students.Length)
            throw new ArgumentException("Seleccione alumnos válidos sin duplicados.");

        // Outer transaction contains the SP's nested transactions and every audit write.
        await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await MassEnrollmentRules.ValidateAvailableAsync(context, procedures, selection, user, ct);
        var rows = await GetMassEnrollmentCandidatesHandler.ReadAsync(procedures, selection, ct);
        foreach (var student in students.Order(StringComparer.Ordinal))
        {
            var eligible = rows.Where(row => row.StudentCode == student).ToArray();
            if (eligible.Length == 0 || eligible.Any(row => row.Enrolled)
                || await MassEnrollmentLookup.IsEnrolledAsync(context, student, selection, ct))
                throw new EnrollmentConflictException("La selección cambió o incluye alumnos ya inscritos. Actualice el listado.");
        }
        foreach (var student in students.Order(StringComparer.Ordinal))
        {
            // Each preceding add may have consumed the final vacancy in this transaction.
            await MassEnrollmentRules.ValidateAvailableAsync(context, procedures, selection, user, ct);
            var parts = selection.Course.Split('/');
            await procedures.SpCre_MovInscAdministrativo_PostgradoAsync(
                student, selection.Period, parts[0], parts[1], "AGREGA", pc, "NO", "NA", parts[2],
                0, 0, "", "PENDIENTE", "", 0, user, null, null, "SADInformes", cancellationToken: ct);
            // The legacy SP has silent no-op paths; an affected-row count is not proof.
            if (!await MassEnrollmentLookup.IsEnrolledAsync(context, student, selection, ct))
                throw new InvalidOperationException("Enrollment postcondition failed.");
        }
        await transaction.CommitAsync(ct);
        return responses.Success(new MassEnrollmentResultDTO(students.Length));
    }
}
