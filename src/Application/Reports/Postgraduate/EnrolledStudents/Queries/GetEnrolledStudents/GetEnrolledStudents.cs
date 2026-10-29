using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudents;

public sealed record GetEnrolledStudents(string? Modality, string? Year)
    : IRequest<ApiResponse<EnrolledStudentsReportDTO>>;

public sealed class GetEnrolledStudentsHandler(
    ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetEnrolledStudents, ApiResponse<EnrolledStudentsReportDTO>>
{
    public async Task<ApiResponse<EnrolledStudentsReportDTO>> Handle(
        GetEnrolledStudents request, CancellationToken ct)
    {
        var modality = EnrolledStudentsCatalog.RequiredModality(request.Modality);
        var year = EnrolledStudentsCatalog.RequiredYear(request.Year);
        var filters = await EnrolledStudentsCatalog.ReadAsync(procedures, ct);
        if (!filters.Years.Contains(year))
            throw new ArgumentException("Seleccione un año válido del catálogo.");

        var data = await procedures.SpRec_AlumnosMatriculadosPostgradoAsync(
            modality, year, cancellationToken: ct) ?? [];
        var rows = data.Select(row => new EnrolledStudentsDTO(
            row.Cod_Alumno?.Trim() ?? "",
            row.Sem_IngresoDecreto?.Trim() ?? "",
            row.Rut?.Trim() ?? "",
            row.Tipo_Alumno?.Trim() ?? "",
            row.Modalidad?.Trim() ?? "",
            row.Cod_SitAcademica?.Trim() ?? "",
            row.Nombres?.Trim() ?? "",
            row.Apellidos?.Trim() ?? "",
            row.Email_Personal?.Trim() ?? "",
            row.Email_Fen?.Trim() ?? "",
            row.Email_laboral?.Trim() ?? "",
            row.Celular?.Trim() ?? "",
            row.Tel_laboral?.Trim() ?? "",
            row.NomEmpresa?.Trim() ?? "",
            row.CargoEmpresa?.Trim() ?? "")).ToArray();
        return responses.Success(new EnrolledStudentsReportDTO(rows, rows.Length));
    }
}
