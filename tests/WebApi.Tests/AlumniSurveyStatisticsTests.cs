using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.AlumniSurveyStatistics.Queries.GetAlumniSurvey;
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

public sealed class AlumniSurveyStatisticsTests
{
    [Fact]
    public async Task Query_maps_all_thirty_two_columns()
    {
        var response = await new GetAlumniSurveyHandler(new Procedures(), new ApiResponseService())
            .Handle(new GetAlumniSurvey(), default);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(32, row.GetType().GetProperties().Length);
        Assert.Equal(new[] {
            "A/1", "Trabajando en el sector privado", "Empresa", "Cargo", "Santiago",
            "Chile", "SI", "NO", "Misma Compañía/Organización", "Está en un nivel más alto",
            "Gano un sueldo más alto", "Emprendimiento", "2020", "Tecnología", "1000",
            "Ventaja Significativa", "Contribución Alta", "Poca Contribución", "No Aplica",
            "Master", "Universidad", "Otro", "1", "Revista", "Conferencia", "Volumen", "2024",
            "SI", "NO", "SI", "Menos de 1.000.000", "02/01/2024 03:04:05",
        }, new[] {
            row.CodAlumno, row.P1, row.P1_1, row.P1_2, row.P1_3, row.P1_4, row.P2, row.P2_1,
            row.P2_2, row.P2_3, row.P2_4, row.P3_1, row.P3_2, row.P3_3, row.P3_4, row.P4,
            row.P5, row.P6, row.P7, row.P8_1, row.P8_2, row.P9, row.P10, row.P10_1,
            row.P10_2, row.P10_3, row.P10_4, row.P11, row.P12, row.P13, row.P14, row.Fecha,
        });
        Assert.Equal(1, response.Data.Total);
    }

    [Fact]
    public async Task Download_requires_permission_and_has_thirty_two_columns()
    {
        var procedures = new Procedures();
        using var factory = CreateFactory(procedures, out var tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync("/api/reports/postgraduate/alumni-survey-statistics/download")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_ALUMNI_SURVEY_STATISTICS"));
        using var response = await client.GetAsync("/api/reports/postgraduate/alumni-survey-statistics/download");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("EncuestaAlumniPostgrado.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(32, rows[0].Descendants(ns + "t").Count());
    }

    [Fact]
    public async Task Download_without_rows_returns_400()
    {
        var procedures = new Procedures { NoData = true };
        using var factory = CreateFactory(procedures, out var tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_ALUMNI_SURVEY_STATISTICS"));
        using var response = await client.GetAsync("/api/reports/postgraduate/alumni-survey-statistics/download");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        Procedures procedures, out TokenFactory tokens)
    {
        var local = new TokenFactory();
        tokens = local;
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                local.Configure(services); local.UseClient(services, "SADInformes");
            });
        });
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("alumni-survey-" + Guid.NewGuid()).Options))
    {
        public bool NoData { get; set; }

        public override Task<List<SpRecEstadisticaEncuestaAlumniResult>?> SpRec_EstadisticaEncuestaAlumniAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (NoData) return Task.FromResult<List<SpRecEstadisticaEncuestaAlumniResult>?>([]);
            return Task.FromResult<List<SpRecEstadisticaEncuestaAlumniResult>?>([
                new() {
                    Cod_Alumno = "A/1", P1 = "Trabajando en el sector privado", P1_1 = "Empresa",
                    P1_2 = "Cargo", P1_3 = "Santiago", P1_4 = "Chile", P2 = "SI", P2_1 = "NO",
                    P2_2 = "Misma Compañía/Organización", P2_3 = "Está en un nivel más alto",
                    P2_4 = "Gano un sueldo más alto", P3_1 = "Emprendimiento", P3_2 = "2020",
                    P3_3 = "Tecnología", P3_4 = "1000", P4 = "Ventaja Significativa",
                    P5 = "Contribución Alta", P6 = "Poca Contribución", P7 = "No Aplica",
                    P8_1 = "Master", P8_2 = "Universidad", P9 = "Otro", P10 = 1,
                    P10_1 = "Revista", P10_2 = "Conferencia", P10_3 = "Volumen", P10_4 = "2024",
                    P11 = "SI", P12 = "NO", P13 = "SI", P14 = "Menos de 1.000.000",
                    Fecha = new DateTime(2024, 1, 2, 3, 4, 5),
                },
            ]);
        }
    }
}
