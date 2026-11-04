using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.Nce.Queries.GetNceReport;

public sealed record GetNceReport : IRequest<ApiResponse<NceReportDTO>>;

public sealed class GetNceReportHandler(ISadContext context, IApiResponseService responses)
    : IRequestHandler<GetNceReport, ApiResponse<NceReportDTO>>
{
    public async Task<ApiResponse<NceReportDTO>> Handle(GetNceReport request, CancellationToken ct)
    {
        var notifications = await (
            from notification in context.NotificacionesCausalEliminacionPostgrado.AsNoTracking()
            join causal in context.CausalesEliminacionPostgrado.AsNoTracking()
                on notification.IdNotificacionCausalEliminacionPostgrado
                equals causal.IdNotificacionCausalEliminacionPostgrado
            join type in context.TiposCausalEliminacionPostgrado.AsNoTracking()
                on causal.IdTipoCausalEliminacionPostgrado equals type.IdTipoCausalEliminacionPostgrado
            where notification.Estado
            orderby notification.Fecha descending, notification.CodigoAlumno
            select new
            {
                notification.IdPublico,
                notification.Fecha,
                Cause = type.Descripcion,
                notification.CodigoAlumno,
            }).ToListAsync(ct);
        var codes = notifications.Select(row => row.CodigoAlumno).Distinct().ToArray();
        var students = await context.Alumnos.AsNoTracking()
            .Where(row => codes.Contains(row.CodAlumno))
            .Select(row => new { row.CodAlumno, row.CodPersona, row.TipoAlumno, row.Modalidad,
                row.CodSitAcademica, row.Motivo })
            .ToListAsync(ct);
        var personCodes = students.Select(row => row.CodPersona).Distinct().ToArray();
        var persons = await context.Personas.AsNoTracking()
            .Where(row => personCodes.Contains(row.CodPersona))
            .Select(row => new { row.CodPersona, row.Nombre1, row.Nombre2, row.Apellido1, row.Apellido2 })
            .ToListAsync(ct);
        var studentByCode = students.ToDictionary(row => row.CodAlumno);
        var personByCode = persons.ToDictionary(row => row.CodPersona);
        var rows = new List<NceReportRowDTO>(notifications.Count);
        foreach (var notification in notifications)
        {
            if (!studentByCode.TryGetValue(notification.CodigoAlumno, out var student)
                || !personByCode.TryGetValue(student.CodPersona, out var person)) continue;
            rows.Add(new NceReportRowDTO(
                notification.IdPublico.ToString().ToUpperInvariant(),
                notification.Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                Text(notification.Cause), Text(notification.CodigoAlumno), Text(student.TipoAlumno),
                Text(student.Modalidad), Text(student.CodSitAcademica), Text(student.Motivo),
                Name(person.Nombre1, person.Nombre2, person.Apellido1, person.Apellido2)));
        }
        return responses.Success(new NceReportDTO(rows.ToArray(), rows.Count));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Name(params string?[] values) =>
        string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))).ToUpperInvariant();
}
