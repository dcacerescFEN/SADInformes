using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Domain.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Queries.GetMassEnrollmentCandidates;

public sealed record GetMassEnrollmentCandidates(MassEnrollmentSelection Selection, string User)
    : IRequest<ApiResponse<MassEnrollmentCandidatesDTO>>;

public sealed class GetMassEnrollmentCandidatesHandler(ISadContext context, ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetMassEnrollmentCandidates, ApiResponse<MassEnrollmentCandidatesDTO>>
{
    public async Task<ApiResponse<MassEnrollmentCandidatesDTO>> Handle(GetMassEnrollmentCandidates request, CancellationToken ct)
    {
        var canEnroll = await MassEnrollmentRules.ValidateAvailableAsync(context, procedures, request.Selection, request.User, ct,
            requireVacancies: false);
        var rows = await ReadAsync(procedures, request.Selection, ct);
        return responses.Success(new MassEnrollmentCandidatesDTO(rows, rows.Count, canEnroll));
    }

    public static async Task<IReadOnlyList<MassEnrollmentCandidateDTO>> ReadAsync(ISadContextProcedures procedures, MassEnrollmentSelection selection, CancellationToken ct)
    {
        var rows = await procedures.SpRec_Alumnos_insc_masiva_catedra_dispAsync(
            selection.StudentType, selection.Modality, selection.Cohort, selection.Course, cancellationToken: ct) ?? [];
        // The legacy LEFT JOIN can repeat a student for earlier enrollments.
        return rows.GroupBy(row => row.Cod_Alumno.Trim()).Select(group =>
        {
            var row = group.First();
            return new MassEnrollmentCandidateDTO(group.Key, row.Nombre?.Trim() ?? "", row.Rut?.Trim() ?? "",
                row.Sem_Ingreso.Trim(), row.Situacion.Trim(), row.Motivo?.Trim() ?? "", row.Decreto.Trim(),
                group.Any(item => item.curso_catedra?.Trim() == "SI"), row.Tipo_Ingreso.Trim());
        }).ToArray();
    }
}
