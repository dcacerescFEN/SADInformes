using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using Domain.Common;

namespace Application.Reports.Requests.UnresolvedPermanence.Queries.GetUnresolvedPermanence;

public sealed record GetUnresolvedPermanence(string? Career)
    : IRequest<ApiResponse<UnresolvedPermanenceReportDTO>>;

public sealed class GetUnresolvedPermanenceHandler(
    ISadContext sad, ISadContextProcedures procedures, IApiResponseService responses)
    : IRequestHandler<GetUnresolvedPermanence, ApiResponse<UnresolvedPermanenceReportDTO>>
{
    public async Task<ApiResponse<UnresolvedPermanenceReportDTO>> Handle(
        GetUnresolvedPermanence request, CancellationToken cancellationToken)
    {
        var career = request.Career?.Trim().ToUpperInvariant() ?? "IC";
        if (career is not ("IC" or "IICG" or "CA"))
            throw new ArgumentException("La carrera no es válida.");
        var today = DateTime.Now.Date;
        var period = await sad.PeriodosAcademicos.AsNoTracking()
            .Where(item => item.Tipo == "SEMESTRAL"
                && item.InicioPeriodo.HasValue && item.FinPeriodo.HasValue
                && item.InicioPeriodo.Value.Date <= today
                && item.FinPeriodo.Value.Date >= today)
            .Select(item => item.Periodo)
            .FirstOrDefaultAsync(cancellationToken) ?? "";
        if (period.Length == 0)
            throw new ArgumentException("No hay un período académico activo.");

        var data = await procedures.SpRec_AlumnosConSolicitudesPermanenciaNoResueltasAsync(
            career, period, cancellationToken: cancellationToken) ?? [];
        var rows = new List<UnresolvedPermanenceRowDTO>(data.Count);
        foreach (var row in data)
        {
            var studentCode = Text(row.Cod_Alumno);
            var previous = await procedures.SpRec_AlumnoSolicitudesAsync(
                studentCode, "PERMANENCIA RESUELTAS", cancellationToken: cancellationToken) ?? [];
            var current = await procedures.SpRec_AlumnoSolicitudesAsync(
                studentCode, "PERMANENCIA PENDIENTES", cancellationToken: cancellationToken) ?? [];
            var core = await procedures.SpRec_AlumnoReprobadasCoreAsync(
                studentCode, cancellationToken: cancellationToken) ?? [];

            rows.Add(new UnresolvedPermanenceRowDTO(
                Text(row.Cod_Persona), studentCode, Text(row.Nombre), Text(row.Situacion),
                Entry(row), Credit(row), Performance(row), Semesters(row),
                string.Join('\n', core.Select(item =>
                    Text(item.Nom_Catedra) + (item.Repitencias > 1 ? $" ({item.Repitencias})" : ""))),
                Units(row), Previous(previous), Current(current), Causes(row)));
        }

        return responses.Success(new UnresolvedPermanenceReportDTO(period, career, rows));
    }

    private static string Entry(Domain.Entities.Sad.Sp.SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult row) =>
        $"Tipo: {Text(row.Tipo_Ingreso)}\nTipo Colegio: {Text(row.TipoColegio)}\nRegión: {Text(row.Region)}";

    private static string Credit(Domain.Entities.Sad.Sp.SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult row) =>
        $"{row.PorcentajeCredito?.ToString("F0", CultureInfo.InvariantCulture) ?? "0"} ({Text(row.PeriodoCredito)})";

    private static string Performance(Domain.Entities.Sad.Sp.SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult row) =>
        $"UDA: {row.UDA ?? 0}\nUDR: {row.UDR ?? 0}\n% UDR: {row.PorcentajeUDR?.ToString("F1", CultureInfo.InvariantCulture) ?? "0.0"}\nProm. Aprob. Rep: {row.PromedioAproREP?.ToString("F1", CultureInfo.InvariantCulture) ?? "0.0"}";

    private static string Semesters(Domain.Entities.Sad.Sp.SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult row) =>
        $"Cursados Ordinarios: {row.SemestresOrdinariosCursados ?? 0}\nCursados Verano: {row.SemestresExtraordinariosCursados ?? 0}\nOrdinarios No Cursados: {row.SemestresOrdinariosNoCursados ?? 0}";

    private static string Units(Domain.Entities.Sad.Sp.SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult row) =>
        $"Período Causal: {Text(row.PeriodoCausalLargo)}\nUD Inscritas Período Causal: {row.UdInscritasPeriodoCausal ?? 0}\nUD Inscritas Período Actual: {row.UdInscritas ?? 0}";

    private static PreviousRequestDTO[] Previous(
        IReadOnlyList<Domain.Entities.Sad.Sp.SpRecAlumnoSolicitudesResult> rows) =>
        rows.Select(row => new PreviousRequestDTO(
            Text(row.PeriodoLargo), Text(row.TipoDescriptivo), Text(row.Resolucion),
            Text(row.Autoridad), string.Equals(row.Resolucion?.Trim(), "Aprobada",
                StringComparison.OrdinalIgnoreCase))).ToArray();

    private static CurrentRequestDTO[] Current(
        IReadOnlyList<Domain.Entities.Sad.Sp.SpRecAlumnoSolicitudesResult> rows) =>
        rows.Select(row => new CurrentRequestDTO(
            Text(row.PeriodoLargo), Text(row.TipoDescriptivo))).ToArray();

    private static EliminationCauseDTO[] Causes(
        Domain.Entities.Sad.Sp.SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult row)
    {
        var values = new List<EliminationCauseDTO>();
        Add(values, row.BajoUdSemestre);
        Add(values, row.BajoUdSemestreNuevas);
        Add(values, row.BajoUdPrimerAño);
        Add(values, row.Reprobadas2a is { Length: > 0 } second ? $"Reprobada(s) Segunda Oportunidad: {second}" : null);
        Add(values, row.Reprobadas3a is { Length: > 0 } third ? $"Reprobada(s) Tercera Oportunidad: {third}" : null);
        Add(values, row.Mas100UdReprobadas);
        Add(values, row.MasDe16Semestres);
        return values.ToArray();
    }

    private static void Add(ICollection<EliminationCauseDTO> values, string? value)
    {
        var text = Text(value);
        if (text.Length > 0) values.Add(new EliminationCauseDTO(text, "danger"));
    }

    private static string Text(string? value) => value?.Trim() ?? "";
}
