using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.ThesisRegistration.Queries.GetThesisRegistration;
using Application.Reports.Postgraduate.ThesisRegistration.Queries.GetThesisRegistrationFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class ThesisRegistrationTests
{
    [Fact]
    public async Task Filters_expose_postgraduate_types_and_dependent_modalities()
    {
        var procedures = new Procedures();
        var result = await new GetThesisRegistrationFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetThesisRegistrationFilters("MBA"), default);
        Assert.Equal(new[] { "Todos", "MBA" }, result.Data.StudentTypes.Select(row => row.Value));
        Assert.Equal(new[] { "Todos", "PT" }, result.Data.Modalities);
        Assert.Equal(new[] { "Todos", "2024" }, result.Data.Years);
        Assert.Equal("MBA", procedures.ModalityType);
    }

    [Fact]
    public async Task Query_maps_all_twenty_one_columns_and_accepts_Todos()
    {
        var procedures = new Procedures();
        var response = await new GetThesisRegistrationHandler(procedures, new ApiResponseService())
            .Handle(new GetThesisRegistration("Todos", "Todos", "Todos"), default);
        Assert.Equal(new[] { "Todos", "Todos", "Todos" }, procedures.Arguments);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "100", "2024F", "D-1", "P/1", "MBA", "PT", "Pérez", "Ana",
            "ENCGE625", "2024F", "Profesor", "6.5", "TESIS1", "2024F", "REGULAR", "Motivo",
            "Tesis", "6.5", "Profesor", "02/01/2024"),
            (row.StudentCode, row.Enrollment, row.EntrySemester, row.Decree, row.PersonCode,
                row.StudentType, row.Modality, row.Surnames, row.Names, row.AfeCourse, row.AfePeriod,
                row.AfeTeacher, row.AfeGrade, row.CourseCode, row.ThesisEnrollmentPeriod,
                row.AcademicSituation, row.Reason, row.ThesisName, row.Grade, row.Teachers, row.ExamDate));
        Assert.Equal(21, row.GetType().GetProperties().Length);
        Assert.Equal(1, response.Data.Total);
    }

    [Fact]
    public async Task Unknown_type_never_calls_the_procedure()
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetThesisRegistrationHandler(procedures, new ApiResponseService())
                .Handle(new GetThesisRegistration("OTHER", "Todos", "Todos"), default));
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Download_requires_permission_and_has_twenty_one_columns()
    {
        var procedures = new Procedures();
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync("/api/reports/postgraduate/thesis-registration/download?studentType=Todos&modality=Todos&year=Todos")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_THESIS_REGISTRATION"));
        using var response = await client.GetAsync(
            "/api/reports/postgraduate/thesis-registration/download?studentType=Todos&modality=Todos&year=Todos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("RegistroTesisPostgrado.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(21, rows[0].Descendants(ns + "t").Count());
    }

    internal sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public string?[]? Arguments { get; private set; }
        public string? ModalityType { get; private set; }

        public override Task<List<SpRecTiposAlumnoXEscuelaResult>?> SpRec_TiposAlumnoXEscuelaAsync(
            string? escuela, string? incluirTodos, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("POSTGRADO", "SI"), (escuela, incluirTodos));
            return Task.FromResult<List<SpRecTiposAlumnoXEscuelaResult>?>([
                new() { Tipo = "Todos", Nombre = "Todos" }, new() { Tipo = "MBA", Nombre = "MBA" },
            ]);
        }

        public override Task<List<SpRecEncuestaGraduadosModalidadesResult>?> spRec_EncuestaGraduados_ModalidadesAsync(
            string? tipo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            ModalityType = tipo;
            return Task.FromResult<List<SpRecEncuestaGraduadosModalidadesResult>?>([
                new() { Modalidad = "Todos" }, new() { Modalidad = "PT" },
            ]);
        }

        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("SI", "Todos"), (incluirTodos, tipoAlumno));
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([
                new() { Año = "Todos" }, new() { Año = "2024" },
            ]);
        }

        public override Task<List<SpRecRegistroTesisPostgradoResult>?> SpRec_RegistroTesisPostgradoAsync(
            string? tipo_Alumno, string? modalidad, string? ingreso,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Arguments = [tipo_Alumno, modalidad, ingreso];
            return Task.FromResult<List<SpRecRegistroTesisPostgradoResult>?>([
                new() { Cod_Alumno = "A/1", Matricula = "100", Sem_IngresoDecreto = "2024F",
                    Decreto = "D-1", Cod_Persona = "P/1", Tipo_Alumno = "MBA", Modalidad = "PT",
                    Apellidos = "Pérez", Nombres = "Ana", CatedraAFE = "ENCGE625",
                    PeriodoAFE = "2024F", ProfesorAFE = "Profesor", NotaAFE = "6.5",
                    Cod_Catedra = "TESIS1", Periodo = "2024F", Cod_SitAcademica = "REGULAR",
                    Motivo = "Motivo", Nombre = "Tesis", Nota = 6.5m, Profesores = "Profesor",
                    Fecha_Examen = new DateTime(2024, 1, 2) },
            ]);
        }
    }
}
