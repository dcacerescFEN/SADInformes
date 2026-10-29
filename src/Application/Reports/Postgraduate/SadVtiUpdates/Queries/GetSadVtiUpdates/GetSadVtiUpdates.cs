using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdates;

public sealed record GetSadVtiUpdates(string? State, string? From, string? To) : IRequest<ApiResponse<SadVtiUpdatesDTO>>;
public sealed class GetSadVtiUpdatesHandler(ISadContext context, IApiResponseService responses)
    : IRequestHandler<GetSadVtiUpdates, ApiResponse<SadVtiUpdatesDTO>>
{
    public async Task<ApiResponse<SadVtiUpdatesDTO>> Handle(GetSadVtiUpdates request, CancellationToken ct)
    {
        if (!DateTime.TryParseExact(request.From, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
            throw new ArgumentException("La fecha desde no es válida.");
        if (!DateTime.TryParseExact(request.To, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
            throw new ArgumentException("La fecha hasta no es válida.");
        if (from > to) throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.");
        var state = request.State ?? "-1";
        if (string.IsNullOrWhiteSpace(state) || state.Length > 5 || state.Any(char.IsControl))
            throw new ArgumentException("El estado no es válido.");
        if (state != "-1" && !await context.AudiEstadoPostgradoAlumnosGuia.AsNoTracking().AnyAsync(row => row.Estado == state, ct))
            throw new ArgumentException("El estado no es válido.");
        var rows = await CreateQuery(context, state, from, to).ToArrayAsync(ct);
        return responses.Success(new SadVtiUpdatesDTO(rows));
    }

    public static IQueryable<SadVtiUpdateDTO> CreateQuery(ISadContext context, string state, DateTime startDate, DateTime endDate) =>
        from audit in context.AudiPostgradoAlumnosGuia.AsNoTracking()
        join status in context.AudiEstadoPostgradoAlumnosGuia.AsNoTracking() on audit.Estado equals status.Estado
        join student in context.Alumnos.AsNoTracking()
            on EF.Functions.Collate(audit.CodAlumnoNuevo ?? audit.CodAlumnoAntiguo, "Modern_Spanish_CI_AS") equals student.CodAlumno
        join person in context.Personas.AsNoTracking() on student.CodPersona equals person.CodPersona
        where audit.FechaRegistro.Date >= startDate.Date && audit.FechaRegistro.Date <= endDate.Date
            && (state == "-1" || audit.Estado == state)
        orderby audit.FechaRegistro, audit.Id
        select new SadVtiUpdateDTO(audit.Estado, status.Descripcion, student.CodPersona,
            audit.CodAlumnoAntiguo, student.CodAlumno,
            person.Apellido2 == null ? null : person.Nombre1 + " " + person.Apellido1 + " " + person.Apellido2,
            student.TipoAlumno, student.Modalidad, audit.CodSitAcademica, audit.Matricula,
            audit.FechaRegistro, audit.MensajeError);
}
