using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.Nce.Queries.GetNceReport;
using Domain.Entities.Sad.Tables.Sad;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class NceTests
{
    [Fact]
    public async Task Report_maps_the_maestro_columns()
    {
        using var context = Context();
        var response = await new GetNceReportHandler(context, new ApiResponseService())
            .Handle(new GetNceReport(), default);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("11111111-1111-1111-1111-111111111111", "02/01/2024 03:04", "Causal A",
            "A1", "MBA", "PT", "ELIM", "Bajo rendimiento", "ANA MARÍA PÉREZ SOTO"),
            (row.Id, row.Date, row.Cause, row.StudentCode, row.StudentType, row.Modality,
                row.AcademicSituation, row.Reason, row.Name));
    }

    [Fact]
    public async Task Download_requires_view_permission_and_has_ten_columns()
    {
        using var context = Context();
        var tokens = new TokenFactory();
        using var factory = Factory(context, tokens);
        using var signing = tokens; using var client = factory.CreateClient();
        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync("/api/reports/postgraduate/nce/download")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_NCE"));
        using var response = await client.GetAsync("/api/reports/postgraduate/nce/download");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(10, rows[0].Descendants(ns + "t").Count());
    }

    [Fact]
    public async Task Download_pdf_requires_view_permission_and_returns_a_pdf()
    {
        using var context = Context();
        var tokens = new TokenFactory();
        using var factory = Factory(context, tokens);
        using var signing = tokens; using var client = factory.CreateClient();
        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync("/api/reports/postgraduate/nce/download/pdf")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_NCE"));
        using var response = await client.GetAsync("/api/reports/postgraduate/nce/download/pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public async Task Download_without_rows_returns_400()
    {
        using var context = Context(empty: true);
        var tokens = new TokenFactory();
        using var factory = Factory(context, tokens);
        using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE_NCE"));
        using var response = await client.GetAsync("/api/reports/postgraduate/nce/download");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> Factory(SadContext context, TokenFactory tokens) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContext>(context);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });

    private static SadContext Context(bool empty = false)
    {
        var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("nce-" + Guid.NewGuid()).Options);
        if (empty) return context;
        var publicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        context.NotificacionesCausalEliminacionPostgrado.Add(new NotificacionCausalEliminacionPostgrado
        {
            IdNotificacionCausalEliminacionPostgrado = 1, IdPublico = publicId, CodigoAlumno = "A1",
            Notificacion = "Correo de notificación", CasillaCordinadora = "coord@fen.cl",
            Fecha = new DateTime(2024, 1, 2, 3, 4, 0), Estado = true,
        });
        context.TiposCausalEliminacionPostgrado.Add(new TipoCausalEliminacionPostgrado
        {
            IdTipoCausalEliminacionPostgrado = 10, Descripcion = "Causal A", Orden = 1, Estado = true,
        });
        context.CausalesEliminacionPostgrado.Add(new CausalEliminacionPostgrado
        {
            IdCausalEliminacionPostgrado = 100, IdTipoCausalEliminacionPostgrado = 10,
            IdNotificacionCausalEliminacionPostgrado = 1,
        });
        context.Alumnos.Add(new Domain.Entities.Sad.Tables.dbo.Alumno
        {
            Matricula = "1", SemIngresoDecreto = "2024F", CodAlumno = "A1", CodPersona = "P1",
            TipoAlumno = "MBA", Modalidad = "PT", CodSitAcademica = "ELIM", Motivo = "Bajo rendimiento",
        });
        context.Personas.Add(new Domain.Entities.Sad.Tables.dbo.Persona
        {
            CodPersona = "P1", Nombre1 = "Ana", Nombre2 = "María", Apellido1 = "Pérez",
            Apellido2 = "Soto",
        });
        context.SaveChanges();
        return context;
    }
}
