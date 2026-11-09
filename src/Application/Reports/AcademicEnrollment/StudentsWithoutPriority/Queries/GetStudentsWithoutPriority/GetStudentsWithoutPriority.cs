using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPriority.Queries.GetStudentsWithoutPriority;

public sealed record GetStudentsWithoutPriority : IRequest<ApiResponse<StudentsWithoutPriorityReportDTO>>;

public sealed class GetStudentsWithoutPriorityHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetStudentsWithoutPriority, ApiResponse<StudentsWithoutPriorityReportDTO>>
{
    public async Task<ApiResponse<StudentsWithoutPriorityReportDTO>> Handle(
        GetStudentsWithoutPriority request, CancellationToken cancellationToken)
    {
        var data = await procedures.SpRec_AlumnosSinPrioridadAsync(
            cancellationToken: cancellationToken) ?? [];

        var rows = data.Select(row => new StudentsWithoutPriorityRowDTO(
            Text(row.Cod_Alumno),
            Text(row.Nombre),
            Text(row.Cod_SitAcademica),
            Text(row.Tipo_Alumno))).ToArray();

        return responses.Success(new StudentsWithoutPriorityReportDTO(rows, rows.Length));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
