using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Commands.EnrollMassEnrollment;

public sealed record EnrollMassEnrollment(EnrollmentRequest Enrollment, string User, string Pc)
    : IRequest<ApiResponse<EnrollmentResult>>;

public sealed class EnrollMassEnrollmentHandler(IMassEnrollmentData data, IApiResponseService responses)
    : IRequestHandler<EnrollMassEnrollment, ApiResponse<EnrollmentResult>>
{
    public async Task<ApiResponse<EnrollmentResult>> Handle(EnrollMassEnrollment request, CancellationToken ct)
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
        await using var transaction = await data.BeginAsync(ct);
        await MassEnrollmentRules.ValidateAvailableAsync(data, selection, user, ct);
        var rows = await data.GetCandidatesAsync(selection, ct);
        foreach (var student in students.Order(StringComparer.Ordinal))
        {
            var eligible = rows.Where(row => row.StudentCode == student).ToArray();
            if (eligible.Length == 0 || eligible.Any(row => row.Enrolled)
                || await data.IsEnrolledAsync(student, selection, ct))
                throw new EnrollmentConflictException("La selección cambió o incluye alumnos ya inscritos. Actualice el listado.");
        }
        foreach (var student in students.Order(StringComparer.Ordinal))
        {
            // Each preceding add may have consumed the final vacancy in this transaction.
            await MassEnrollmentRules.ValidateAvailableAsync(data, selection, user, ct);
            await data.AddAsync(student, selection, user, pc, ct);
            // The legacy SP has silent no-op paths; an affected-row count is not proof.
            if (!await data.IsEnrolledAsync(student, selection, ct))
                throw new InvalidOperationException("Enrollment postcondition failed.");
        }
        await transaction.CommitAsync(ct);
        return responses.Success(new EnrollmentResult(students.Length));
    }
}
