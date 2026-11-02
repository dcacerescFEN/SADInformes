using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReport;

public sealed record GetAfeReport(string? Period, string? Course, string? Advance)
    : IRequest<ApiResponse<AfeReportDTO>>;

public sealed class GetAfeReportHandler(
    ISadContextProcedures procedures, ISadContext sad, IEnumerable<IDocenciaWebContext> docencia,
    IApiResponseService responses)
    : IRequestHandler<GetAfeReport, ApiResponse<AfeReportDTO>>
{
    public async Task<ApiResponse<AfeReportDTO>> Handle(GetAfeReport request, CancellationToken ct)
    {
        var db = docencia.LastOrDefault()
            ?? throw new InvalidOperationException("La conexión DocenciaWeb no está configurada.");
        var period = request.Period?.Trim();
        if (string.IsNullOrEmpty(period)) throw new ArgumentException("Seleccione un período.");
        var periods = await procedures.SpRec_PeriodosAcademicosProgramacionAsync(
            "20040", 0, "NO", "POSTGRADO", cancellationToken: ct) ?? [];
        if (!periods.Any(row => string.Equals(row.Periodo?.Trim(), period, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("El período no es válido.");
        var parts = ParseCourse(request.Course);

        var allTasks = (await procedures.SpRec_ObtenerTareasAFEReporteAsync(period, cancellationToken: ct) ?? [])
            .Where(row => row.Cod_Catedra.Trim() == parts.Course && row.Cod_Seccion.Trim() == parts.Section)
            .ToList();
        if (allTasks.Count == 0) return responses.Success(new AfeReportDTO([], 0));
        var order = allTasks.Select((task, index) => new { task.Id, Value = allTasks.Count - index })
            .ToDictionary(row => row.Id, row => row.Value);
        var selected = SelectTasks(allTasks, request.Advance);
        var taskIds = selected.Select(row => row.Id).ToArray();

        var students = await (from movement in sad.MovimientosInscripcions.AsNoTracking()
            join alumno in sad.Alumnos.AsNoTracking() on movement.CodAlumno equals alumno.CodAlumno
            join persona in sad.Personas.AsNoTracking() on alumno.CodPersona equals persona.CodPersona
            where movement.CodCatedra == parts.Course && movement.CodSeccion == parts.Section
                && movement.Periodo == period && movement.TipoMovimiento == "AGREGA"
                && movement.Estado == "ACEPTA"
            orderby persona.Apellido1, persona.Nombre1
            select new { alumno.CodAlumno, persona.Uname, persona.Apellido1, persona.Apellido2, persona.Nombre1 })
            .ToListAsync(ct);
        var studentRows = students.Select(row => new {
            row.CodAlumno,
            Uname = row.Uname?.Trim() ?? "",
            Name = Join(row.Apellido1, row.Apellido2, row.Nombre1),
        }).ToList();

        var deliveries = await db.TaskEntregas.AsNoTracking()
            .Where(row => taskIds.Contains(row.TaskId) && row.FechaElimina == null)
            .OrderBy(row => row.Id)
            .Select(row => new DeliveryRow(row.TaskId, row.UnameAlumno, row.FechaEntrega, row.Nota,
                row.FechaEvaluacion, row.ObservacionProfesor))
            .ToListAsync(ct);
        var groupRows = await (from groupRow in db.TaskGrupos.AsNoTracking()
            join member in db.TaskGrupoAlumnos.AsNoTracking() on groupRow.Id equals member.GrupoId
            where taskIds.Contains(groupRow.TaskId) && groupRow.FechaElimina == null && member.FechaElimina == null
            select new { groupRow.TaskId, member.UnameAlumno, groupRow.Orden }).ToListAsync(ct);
        var groups = groupRows
            .GroupBy(row => (row.TaskId, row.UnameAlumno))
            .ToDictionary(group => group.Key, group => group.First().Orden?.ToString(CultureInfo.InvariantCulture) ?? "");

        var rows = new List<AfeReportRowDTO>();
        foreach (var task in selected)
        {
            var taskDeliveries = deliveries.Where(row => row.TaskId == task.Id)
                .GroupBy(row => row.UnameAlumno).Select(group => group.First()).ToList();
            if (studentRows.Count == 0)
            {
                rows.Add(Row(task, order[task.Id], null, null, null));
                continue;
            }
            var delivered = new HashSet<string>(StringComparer.Ordinal);
            foreach (var delivery in taskDeliveries)
            {
                var student = studentRows.FirstOrDefault(row => row.Uname == delivery.UnameAlumno.Trim());
                if (student is null) continue;
                delivered.Add(student.Uname);
                groups.TryGetValue((task.Id, student.Uname), out var group);
                rows.Add(Row(task, order[task.Id], student.Name, delivery, group));
            }
            foreach (var student in studentRows.Where(row => !delivered.Contains(row.Uname)))
            {
                groups.TryGetValue((task.Id, student.Uname), out var group);
                rows.Add(Row(task, order[task.Id], student.Name, null, null));
            }
        }
        return responses.Success(new AfeReportDTO(rows.ToArray(), rows.Count));
    }

    private static List<Domain.Entities.Sad.Sp.SpRecObtenerTareasAfeReporteResult> SelectTasks(
        List<Domain.Entities.Sad.Sp.SpRecObtenerTareasAfeReporteResult> tasks, string? advance)
    {
        if (string.IsNullOrWhiteSpace(advance) || advance.Trim() == "-1") return tasks;
        if (!long.TryParse(advance.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            || !tasks.Any(task => task.Id == id))
            throw new ArgumentException("El número de avance no es válido.");
        return tasks.Where(task => task.Id == id).ToList();
    }

    private static AfeReportRowDTO Row(
        Domain.Entities.Sad.Sp.SpRecObtenerTareasAfeReporteResult task, int order, string? student,
        DeliveryRow? delivery, string? group) =>
        new(order.ToString(CultureInfo.InvariantCulture), task.Titulo?.Trim() ?? "",
            Date(task.Fecha_creacion), Date(task.Fecha_Entrega), Teacher(task),
            $"{task.Nom_Catedra?.Trim()} ({task.Cod_Catedra.Trim()}/{task.Cod_Seccion.Trim()})",
            student ?? "", group ?? "", Date(delivery?.FechaEntrega), Text(delivery?.Nota),
            Date(delivery?.FechaEvaluacion), Text(delivery?.ObservacionProfesor));

    private static (string Course, string Section) ParseCourse(string? value)
    {
        var parts = value?.Trim().Split('/');
        if (parts is not { Length: 2 } || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new ArgumentException("Seleccione un curso y sección válidos.");
        return (parts[0].Trim(), parts[1].Trim());
    }

    private static string Join(params string?[] values) =>
        string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
    private static string Teacher(Domain.Entities.Sad.Sp.SpRecObtenerTareasAfeReporteResult task) =>
        Join(task.Apellido1, task.Apellido2, task.Nombre1);
    private static string Text(string? value) => value?.Trim() ?? "";
    private static string Date(DateTime? value) => value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";

    private sealed record DeliveryRow(long TaskId, string UnameAlumno, DateTime? FechaEntrega,
        string? Nota, DateTime? FechaEvaluacion, string? ObservacionProfesor);
}
