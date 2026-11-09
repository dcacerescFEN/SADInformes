using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReport;
using Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReportAdvances;
using Application.Reports.Postgraduate.AfeReport.Queries.GetAfeReportFilters;
using Domain.Common;
using Domain.Entities.DocenciaWeb;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class AfeReportTests
{
    [Fact]
    public async Task Filters_expose_postgraduate_periods_types_and_dependent_courses()
    {
        var procedures = new Procedures();
        var response = await new GetAfeReportFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetAfeReportFilters(null, null, null), default);
        Assert.Equal(new[] { "2024A" }, response.Data.Periods.Select(row => row.Value));
        Assert.Equal(new[] { "Todos", "MBA" }, response.Data.StudentTypes.Select(row => row.Value));
        Assert.Equal(new[] { "Todos" }, response.Data.Modalities.Select(row => row.Value));
        Assert.Equal(new[] { "ENECO775/1" }, response.Data.Courses.Select(row => row.Value));
    }

    [Fact]
    public async Task Advances_expose_course_tasks_with_Todos_when_there_are_several()
    {
        using var docencia = Docencia();
        var response = await new GetAfeReportAdvancesHandler(
            new Procedures(), [docencia], new ApiResponseService())
            .Handle(new GetAfeReportAdvances("2024A", "ENECO775/1"), default);
        Assert.Equal(new[] { "-1", "10", "20" }, response.Data.Advances.Select(row => row.Value));
        Assert.Equal(new[] { "Todos", "1", "2" }, response.Data.Advances.Select(row => row.Text));
    }

    [Fact]
    public async Task Report_maps_tasks_students_deliveries_and_missing_students()
    {
        using var sad = Sad();
        using var docencia = Docencia();
        var response = await new GetAfeReportHandler(
            new Procedures(), sad, [docencia], new ApiResponseService())
            .Handle(new GetAfeReport("2024A", "ENECO775/1", "-1"), default);
        Assert.Equal(4, response.Data.Total);
        var first = response.Data.Rows[0];
        Assert.Equal(new[] { "2", "Avance dos", "02/01/2024", "03/01/2024", "Profesor Uno",
            "Economía (ENECO775/1)", "Pérez Ana", "3", "04/01/2024", "6.5", "05/01/2024",
            "Buen trabajo" }, new[] { first.Advance, first.Title, first.CreatedAt, first.DueAt,
            first.Teacher, first.Course, first.Student, first.Group, first.DeliveredAt, first.Grade,
            first.EvaluatedAt, first.EvaluationObservation });
        Assert.Equal(("2", "Soto Luis", ""), (response.Data.Rows[1].Advance,
            response.Data.Rows[1].Student, response.Data.Rows[1].Group));
        Assert.Equal(("1", "Pérez Ana", ""), (response.Data.Rows[2].Advance,
            response.Data.Rows[2].Student, response.Data.Rows[2].Group));
    }

    [Fact]
    public async Task Download_requires_permission_and_has_twelve_columns()
    {
        using var sad = Sad();
        using var docencia = Docencia();
        var procedures = new Procedures();
        var tokens = new TokenFactory();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                services.AddSingleton<ISadContext>(sad);
                services.AddSingleton<IDocenciaWebContext>(docencia);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        using var signing = tokens; using var client = factory.CreateClient();
        const string url = "/api/reports/postgraduate/afe-report/download?period=2024A&course=ENECO775%2F1&advance=-1";
        Assert.Equal((int)HttpStatusCode.Unauthorized, (int)(await client.GetAsync(url)).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE"));
        using var response = await client.GetAsync(url);
        var errorBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, errorBody);
        Assert.Contains("Reporte nivel curso AFE - coordinacion.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(12, rows[0].Descendants(ns + "t").Count());
    }

    [Fact]
    public void Task_identifiers_use_64_bit_types()
    {
        Assert.Equal(typeof(long), typeof(SpRecAfePorPeriodoTipoAlumnoResult)
            .GetProperty(nameof(SpRecAfePorPeriodoTipoAlumnoResult.Id))!.PropertyType);
        Assert.Equal(typeof(long), typeof(SpRecObtenerTareasAfeReporteResult)
            .GetProperty(nameof(SpRecObtenerTareasAfeReporteResult.Id))!.PropertyType);
        Assert.Equal(typeof(long), typeof(TaskM).GetProperty(nameof(TaskM.Id))!.PropertyType);
        Assert.Equal(typeof(long), typeof(TaskEntrega)
            .GetProperty(nameof(TaskEntrega.TaskId))!.PropertyType);
    }

    private static SadContext Sad()
    {
        var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("afe-sad-" + Guid.NewGuid()).Options);
        context.Alumnos.AddRange(
            new Alumno { Matricula = "1", SemIngresoDecreto = "2024F", CodAlumno = "A1",
                CodPersona = "P1", TipoAlumno = "MBA" },
            new Alumno { Matricula = "2", SemIngresoDecreto = "2024F", CodAlumno = "A2",
                CodPersona = "P2", TipoAlumno = "MBA" });
        context.Personas.AddRange(
            new Persona { CodPersona = "P1", Nombre1 = "Ana", Apellido1 = "Pérez", Uname = "a1" },
            new Persona { CodPersona = "P2", Nombre1 = "Luis", Apellido1 = "Soto", Uname = "a2" });
        context.MovimientosInscripcions.AddRange(
            new MovimientosInscripcion { Identificacion = 1, CodAlumno = "A1", Periodo = "2024A",
                CodCatedra = "ENECO775", CodSeccion = "1", TipoMovimiento = "AGREGA", Estado = "ACEPTA" },
            new MovimientosInscripcion { Identificacion = 2, CodAlumno = "A2", Periodo = "2024A",
                CodCatedra = "ENECO775", CodSeccion = "1", TipoMovimiento = "AGREGA", Estado = "ACEPTA" });
        context.SaveChanges();
        return context;
    }

    private static DocenciaWebContext Docencia()
    {
        var context = new DocenciaWebContext(new DbContextOptionsBuilder<DocenciaWebContext>()
            .UseInMemoryDatabase("afe-docencia-" + Guid.NewGuid()).Options);
        context.TaskTipos.Add(new TaskTipo { Id = 1, TipoTask = "AFE" });
        context.TaskMs.AddRange(
            new TaskM { Id = 20, CodCatedra = "ENECO775", CodSeccion = "1", Periodo = "2024A",
                TipoTaskId = 1 },
            new TaskM { Id = 10, CodCatedra = "ENECO775", CodSeccion = "1", Periodo = "2024A",
                TipoTaskId = 1 });
        context.TaskEntregas.Add(new TaskEntrega { Id = 1, TaskId = 20, UnameAlumno = "a1",
            FechaEntrega = new DateTime(2024, 1, 4), Nota = "6.5",
            FechaEvaluacion = new DateTime(2024, 1, 5), ObservacionProfesor = "Buen trabajo" });
        context.TaskGrupos.Add(new TaskGrupo { Id = 1, TaskId = 20, Orden = 3 });
        context.TaskGrupoAlumnos.Add(new TaskGrupoAlumno { GrupoId = 1, UnameAlumno = "a1" });
        context.SaveChanges();
        return context;
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("afe-procedures-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosAcademicosProgramacionResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 - Primer Semestre" },
            ]);

        public override Task<List<SpRecTiposAlumnoXEscuelaResult>?> SpRec_TiposAlumnoXEscuelaAsync(
            string? escuela, string? incluirTodos, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecTiposAlumnoXEscuelaResult>?>([
                new() { Tipo = "Todos", Nombre = "Todos" }, new() { Tipo = "MBA", Nombre = "MBA" },
            ]);

        public override Task<List<SpRecEncuestaGraduadosModalidadesResult>?> spRec_EncuestaGraduados_ModalidadesAsync(
            string? tipo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEncuestaGraduadosModalidadesResult>?>([
                new() { Modalidad = "Todos" },
            ]);

        public override Task<List<SpRecAfePorPeriodoTipoAlumnoResult>?> SpRec_AfePorPeriodoTipoAlumnoAsync(
            string? periodo, string? tipo, string? modalidad, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAfePorPeriodoTipoAlumnoResult>?>([
                new() { Id = 10, Curso = "ENECO775/1" }, new() { Id = 20, Curso = "ENECO775/1" },
            ]);

        public override Task<List<SpRecObtenerTareasAfeReporteResult>?> SpRec_ObtenerTareasAFEReporteAsync(
            string? periodo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecObtenerTareasAfeReporteResult>?>([
                new() { Id = 20, Cod_Catedra = "ENECO775", Cod_Seccion = "1", Periodo = "2024A",
                    Titulo = "Avance dos", Fecha_creacion = new DateTime(2024, 1, 2),
                    Fecha_Entrega = new DateTime(2024, 1, 3), Nombre1 = "Uno", Apellido1 = "Profesor",
                    Nom_Catedra = "Economía" },
                new() { Id = 10, Cod_Catedra = "ENECO775", Cod_Seccion = "1", Periodo = "2024A",
                    Titulo = "Avance uno", Fecha_creacion = new DateTime(2024, 1, 1),
                    Fecha_Entrega = new DateTime(2024, 1, 2), Nombre1 = "Uno", Apellido1 = "Profesor",
                    Nom_Catedra = "Economía" },
            ]);
    }
}
