using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.EntrySurveyStatistics.Queries.GetEntrySurveyStatistics;
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

public sealed class EntrySurveyStatisticsTests
{
    [Fact]
    public async Task Query_maps_all_forty_four_visible_columns()
    {
        var response = await new GetEntrySurveyStatisticsHandler(new Procedures(), new ApiResponseService())
            .Handle(new GetEntrySurveyStatistics(), default);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "MBA", "PT", "5", "Poco Importante", "En desacuerdo",
            "Empresa", "Público", "Educación", "Primer nivel", "20-25", "Santiago",
            "01/02/2024 10:00:00"),
            (row.CodAlumno, row.StudentType, row.Modality, row.Q1, row.Q2_1, row.Q3_1,
                row.Q7, row.Q8, row.Q9, row.Q12, row.Q14, row.Q16, row.Fecha));
        Assert.Equal(44, row.GetType().GetProperties().Length);
    }

    [Fact]
    public async Task Empty_procedure_result_is_valid()
    {
        var response = await new GetEntrySurveyStatisticsHandler(
                new Procedures { Empty = true }, new ApiResponseService())
            .Handle(new GetEntrySurveyStatistics(), default);
        Assert.Empty(response.Data.Rows);
        Assert.Equal(0, response.Data.Total);
    }

    [Fact]
    public async Task Download_requires_permission_and_has_forty_four_columns()
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
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_ENTRY_SURVEY_STATISTICS"));
        using var response = await client.GetAsync("/api/reports/postgraduate/entry-survey-statistics/download");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("EncuestaEntradaPostgrado.xlsx", response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(xml);
        var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(44, rows[0].Descendants(ns + "t").Count());
        Assert.Equal(44, rows[1].Elements(ns + "c").Count());
    }

    internal sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public bool Empty { get; init; }
        public override Task<List<SpRecEstadisticaEncuestaEntradaResult>?> SpRec_EstadisticaEncuestaEntradaAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEstadisticaEncuestaEntradaResult>?>(Empty ? null :
            [
                new()
                {
                    Cod_Alumno = "A/1", Tipo_Alumno = "MBA", Modalidad = "PT", Q1 = "5",
                    Q2_1 = "Poco Importante", Q2_2 = "Nada Importante", Q2_3 = "Algo Importante",
                    Q2_4 = "Indiferente", Q2_5 = "Muy Importante", Q2_6 = "Poco Importante",
                    Q2_7 = "Nada Importante", Q2_8 = "Algo Importante", Q3_1 = "En desacuerdo",
                    Q3_2 = "Muy en desacuerdo", Q3_3 = "De acuerdo", Q3_4 = "Neutral",
                    Q3_5 = "Muy de acuerdo", Q3_6 = "En desacuerdo", Q3_7 = "Neutral",
                    Q4_1 = "Universidad", Q4_2 = "En desacuerdo", Q4_3 = "Neutral",
                    Q4_4 = "De acuerdo", Q4_5 = "Muy de acuerdo", Q4_6 = "Neutral",
                    Q4_7 = "En desacuerdo", Q4_8 = "De acuerdo", Q5 = "Correo electrónico",
                    Q5_otro = "", Q6 = "Ejecutivo (trabajador dependiente)", Q7 = "Empresa",
                    Q8 = "Público", Q9 = "Educación", Q9_otro = "", Q10 = "Empresa Grande (201 trabajadores o más)",
                    Q11 = "Jefe", Q11_otro = "", Q12 = "Primer nivel", Q13 = "Entre 1.000.000 y 1.500.000",
                    Q14 = "20-25", Q15 = "Metropolitana", Q16 = "Santiago", Q17 = "Providencia",
                    Fecha = new DateTime(2024, 2, 1, 10, 0, 0),
                },
            ]);
    }
}
