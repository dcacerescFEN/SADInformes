using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalData;
using Application.Reports.Postgraduate.PersonalData.Queries.GetPersonalDataFilters;
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

public sealed class PersonalDataTests
{
    [Fact]
    public async Task Filters_expose_postgraduate_types_and_dependent_modalities()
    {
        var procedures = new Procedures();
        var result = await new GetPersonalDataFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetPersonalDataFilters("MBA"), default);
        Assert.Equal(new[] { "Todos", "MBA" }, result.Data.StudentTypes.Select(row => row.Value));
        Assert.Equal(new[] { "Todos", "PT" }, result.Data.Modalities);
        Assert.Equal(new[] { "Todos", "2024" }, result.Data.Years);
        Assert.Equal("MBA", procedures.ModalityType);
    }

    [Fact]
    public async Task Query_maps_all_twenty_six_columns_and_accepts_Todos()
    {
        var procedures = new Procedures();
        var response = await new GetPersonalDataHandler(procedures, new ApiResponseService())
            .Handle(new GetPersonalData("Todos", "Todos", "Todos"), default);
        Assert.Equal(new[] { "Todos", "Todos", "Todos" }, procedures.Arguments);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("1", "A/1", "2024F", "P/1", "MBA", "PT", "REGULAR", "Motivo",
            "Ana", "Pérez", "ana@example.test", "ana@fen.uchile.cl", "ana@work.test",
            "+569", "+562", "Empresa", "Cargo", "01/02/1990", "Chilena", "F",
            "Carrera", "Universidad", "01/01/2024", "02/01/2024", "Otoño 2024", "Verano 2024"),
            (row.IdAlumno, row.StudentCode, row.EntrySemester, row.PersonCode, row.StudentType,
                row.Modality, row.AcademicSituation, row.Reason, row.Names, row.Surnames,
                row.PersonalEmail, row.FenEmail, row.WorkEmail, row.Mobile, row.WorkPhone,
                row.Company, row.Position, row.BirthDate, row.Nationality, row.Sex,
                row.PreviousDegree, row.OriginUniversity, row.ResolutionDate, row.ExamDate,
                row.LastCoursePeriod, row.ExamPeriod));
        Assert.Equal(26, row.GetType().GetProperties().Length);
        Assert.Equal(1, response.Data.Total);
    }

    [Fact]
    public async Task Unknown_type_never_calls_the_procedure()
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetPersonalDataHandler(procedures, new ApiResponseService())
                .Handle(new GetPersonalData("OTHER", "Todos", "Todos"), default));
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Download_requires_permission_and_has_twenty_six_columns()
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
            (int)(await client.GetAsync("/api/reports/postgraduate/personal-data/download?studentType=Todos&modality=Todos&year=Todos")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_PERSONAL_DATA"));
        using var response = await client.GetAsync(
            "/api/reports/postgraduate/personal-data/download?studentType=Todos&modality=Todos&year=Todos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Datos_Personales_Postgrado.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(26, rows[0].Descendants(ns + "t").Count());
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptions<SadContext>()))
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

        public override Task<List<SpRecAlumnosDatosPersonalesPostgradoResult>?> SpRec_AlumnosDatosPersonalesPostgradoAsync(
            string? tipo_Alumno, string? modalidad, string? ingreso,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Arguments = [tipo_Alumno, modalidad, ingreso];
            return Task.FromResult<List<SpRecAlumnosDatosPersonalesPostgradoResult>?>([
                new() { ID_Alumno = 1, Cod_Alumno = "A/1", Sem_IngresoDecreto = "2024F",
                    Cod_Persona = "P/1", Tipo_Alumno = "MBA", Modalidad = "PT",
                    Cod_SitAcademica = "REGULAR", Motivo = "Motivo", Nombres = "Ana",
                    Apellidos = "Pérez", Email_Personal = "ana@example.test",
                    Email_Fen = "ana@fen.uchile.cl", Email_laboral = "ana@work.test",
                    Celular = "+569", Tel_laboral = "+562", NomEmpresa = "Empresa",
                    CargoEmpresa = "Cargo", Fec_Nacimiento = new DateTime(1990, 2, 1),
                    Nacionalidad = "Chilena", Sexo = "F", CarreraAnterior = "Carrera",
                    Universidad_de_Procedencia = "Universidad", FechaResolucion = "01/01/2024",
                    Fecha_Examen = "02/01/2024", Ultimo_Periodo_Inscripcion_Curso = "Otoño 2024",
                    Periodo_Examen = "Verano 2024" },
            ]);
        }
    }
}
