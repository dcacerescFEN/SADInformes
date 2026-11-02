using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.CourseActivities.Queries.GetActivities;
using Application.Reports.Postgraduate.CourseActivities.Queries.GetActivityFilters;
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

public sealed class CourseActivitiesTests
{
    [Fact]
    public async Task Filters_and_default_year_map_the_activity_report()
    {
        var procedures = new Procedures();
        var filters = await new GetActivityFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetActivityFilters(), default);
        Assert.Equal(new[] { "2024", "2023" }, filters.Data.Years.Select(row => row.Value));

        var response = await new GetActivitiesHandler(procedures, new ApiResponseService())
            .Handle(new GetActivities(null), default);
        Assert.Equal("2024", response.Data.Year);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(new[] { "1", "Clase", "Álgebra", "01-01-2024 10:00:00", "01-01-2024 12:00:00",
            "2024A", "ENECO775", "1", "MBA", "PT", "2024", "Sala 1", "30", "25", "Sala",
            "Sí", "01-01-2024 09:00:00", "usuario", "Falso", "02-01-2024 09:00:00", "usuario2",
            "Advertencia x Capacidad Sala" },
            new[] { row.Id, row.Type, row.Course, row.From, row.To, row.Period, row.CourseCode,
                row.Section, row.Program, row.Modality, row.Promotion, row.Room, row.RoomCapacity,
                row.EnrolledStudents, row.RoomType, row.HybridRoom, row.EntryDate, row.EntryUser,
                row.Suspended, row.SuspensionDate, row.SuspensionUser, row.Observations });
        Assert.Equal(22, row.GetType().GetProperties().Length);
    }

    [Fact]
    public async Task Download_requires_permission_and_has_twenty_two_columns()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens; using var client = factory.CreateClient();
        const string url = "/api/reports/postgraduate/course-activities/download?year=2024";
        Assert.Equal((int)HttpStatusCode.Unauthorized, (int)(await client.GetAsync(url)).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_COURSE_ACTIVITIES"));
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Reporte Actividades Cursos Postgrado - 2024.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(22, rows[0].Descendants(ns + "t").Count());
    }

    [Fact]
    public async Task Download_without_rows_returns_400()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures { NoData = true }, tokens);
        using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_COURSE_ACTIVITIES"));
        using var response = await client.GetAsync(
            "/api/reports/postgraduate/course-activities/download?year=2024");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(Procedures procedures, TokenFactory tokens) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("course-activities-" + Guid.NewGuid()).Options))
    {
        public bool NoData { get; set; }

        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEnrollmentYearsResult>?>([
                new() { Año = "2024" }, new() { Año = "2023" },
            ]);

        public override Task<List<SpRecReporteActividadesCursosPostgradoResult>?> SpRec_ReporteActividadesCursosPostgradoAsync(
            int? anno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(2024, anno);
            if (NoData) return Task.FromResult<List<SpRecReporteActividadesCursosPostgradoResult>?>([]);
            return Task.FromResult<List<SpRecReporteActividadesCursosPostgradoResult>?>([
                new() { Id = 1, Tipo = "Clase", Catedra = "Álgebra", Desde = "01-01-2024 10:00:00",
                    Hasta = "01-01-2024 12:00:00", Periodo = "2024A", Cod_Catedra = "ENECO775",
                    Cod_Seccion = "1", Programa = "MBA", Modalidad = "PT", Promocion = "2024",
                    Sala = "Sala 1", Capacidad_Sala = 30, Estudiantes_Inscritos = 25, Tipo_Sala = "Sala",
                    Es_Sala_Hibrida = "Sí", Fecha_Ingreso = "01-01-2024 09:00:00",
                    UsuarioIngreso = "usuario", Suspendida = "Falso",
                    Fecha_Suspención = new DateTime(2024, 1, 2, 9, 0, 0), Usuario_Suspención = "usuario2",
                    Observaciones = "Advertencia x Capacidad Sala" },
            ]);
        }
    }
}
