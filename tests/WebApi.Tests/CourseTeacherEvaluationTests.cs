using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.CourseTeacherEvaluation.Queries.GetCourseTeacherEvaluation;
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

public sealed class CourseTeacherEvaluationTests
{
    [Fact]
    public async Task Executive_query_maps_the_course_shape()
    {
        var procedures = new Procedures();
        var response = await new GetCourseTeacherEvaluationHandler(procedures, new ApiResponseService())
            .Handle(new GetCourseTeacherEvaluation("2024", "EX"), default);
        Assert.Equal(("2024", "MBA600", "01", "P/1", "Ana Pérez", "5.25", "6.00", "80%", "6.00"),
            (response.Data.Executive[0].Period, response.Data.Executive[0].CourseCode,
                response.Data.Executive[0].Section, response.Data.Executive[0].ProfessorCode,
                response.Data.Executive[0].ProfessorName, response.Data.Executive[0].C1_1,
                response.Data.Executive[0].P1_1, response.Data.Executive[0].CoursePercentage,
                response.Data.Executive[0].Grade));
        Assert.Empty(response.Data.FullTime);
    }

    [Fact]
    public async Task FullTime_query_maps_the_twenty_three_question_shape()
    {
        var response = await new GetCourseTeacherEvaluationHandler(
                new Procedures(), new ApiResponseService())
            .Handle(new GetCourseTeacherEvaluation("2024", "FT"), default);
        Assert.Equal("FT", response.Data.Type);
        Assert.Equal("5.00", response.Data.FullTime[0].C1_23);
        Assert.Empty(response.Data.Executive);
    }

    [Theory]
    [InlineData("202", "EX")]
    [InlineData("2024", "OTHER")]
    [InlineData("2024", "")]
    public async Task Invalid_selection_is_rejected(string year, string type)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetCourseTeacherEvaluationHandler(new Procedures(), new ApiResponseService())
                .Handle(new GetCourseTeacherEvaluation(year, type), default));
    }

    [Fact]
    public async Task Download_requires_permission_and_uses_the_legacy_filename()
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
            (int)(await client.GetAsync("/api/reports/postgraduate/course-teacher-evaluation/download?year=2024&type=EX")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes",
            "PERMISSION_REPORT_VIEW_POSTGRADUATE_COURSE_TEACHER_EVALUATION"));
        using var response = await client.GetAsync(
            "/api/reports/postgraduate/course-teacher-evaluation/download?year=2024&type=EX");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Evaluacion_Docente_Curso.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(31, rows[0].Descendants(ns + "t").Count());
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptions<SadContext>()))
    {
        public override Task<List<SpRecEvaPostgradoCursosDocentesResult>?> SpRec_EvaPostgradoCursosDocentesAsync(
            string? año, string? tipo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEvaPostgradoCursosDocentesResult>?>([
                new() { Periodo = "2024", Cod_Catedra = "MBA600", Cod_Seccion = "01",
                    Cod_Profesor = "P/1", Nombre = "Ana Pérez", Nom_Catedra = "Curso",
                    ModalidadPostgrado = "PT", Tipo_AlumnoPostgrado = "MBA", Inscritos = 10,
                    ContestadasCurso = 8, PorcentajeCurso = "80%", ContestadasDocente = 9,
                    PorcentajeDocente = "90%", C1_1 = 5.25, C1_2 = 5, C1_3 = 5, C1_4 = 5,
                    P1_1 = 6, P1_2 = 6, P1_3 = 6, P1_4 = 6, P1_5 = 6, P1_6 = 6,
                    P1_7 = 6, P1_8 = 6, P1_9 = 6, P1_10 = 6, P1_11 = 6, P1_12 = 6,
                    P1_13 = 6, Nota = 6 },
            ]);
        public override Task<List<SpRecEvaPostgradoCursosDocentesFullTimeResult>?> SpRec_EvaPostgradoCursosDocentesFullTimeAsync(
            string? año, string? tipo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEvaPostgradoCursosDocentesFullTimeResult>?>([
                new() { Periodo = "2024", Cod_Catedra = "MBA600", Cod_Seccion = "01",
                    Cod_Profesor = "P/1", Nombre = "Ana Pérez", C1_1 = 5, C1_2 = 5, C1_3 = 5,
                    C1_4 = 5, C1_5 = 5, C1_6 = 5, C1_7 = 5, C1_8 = 5, C1_9 = 5, C1_10 = 5,
                    C1_11 = 5, C1_12 = 5, C1_13 = 5, C1_14 = 5, C1_15 = 5, C1_16 = 5,
                    C1_17 = 5, C1_18 = 5, C1_19 = 5, C1_20 = 5, C1_21 = 5, C1_22 = 5,
                    C1_23 = 5, P1_1 = 6, P1_2 = 6, P1_3 = 6, P1_4 = 6, P1_5 = 6, P1_6 = 6,
                    P1_7 = 6, P1_8 = 6, P1_9 = 6, P1_10 = 6, P1_11 = 6, P1_12 = 6,
                    P1_13 = 6, Nota = 6 },
            ]);
    }
}
