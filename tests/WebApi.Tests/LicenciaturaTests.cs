using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.Licenciatura.Queries.GetLicenciatura;
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

public sealed class LicenciaturaTests
{
    [Fact]
    public async Task Query_maps_the_five_report_columns()
    {
        var response = await new GetLicenciaturaHandler(new Procedures(), new ApiResponseService())
            .Handle(new GetLicenciatura(), default);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "Ana Pérez", "Ingeniería Comercial", "Licenciado en Ciencias", "Cumple"),
            (row.StudentCode, row.Name, row.Career, row.Degree, row.Status));
        Assert.Equal(1, response.Data.Total);
    }

    [Fact]
    public async Task Download_requires_permission_and_has_five_columns()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens; using var client = factory.CreateClient();
        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync("/api/reports/postgraduate/licenciatura-pregrado/download")).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE"));
        using var response = await client.GetAsync("/api/reports/postgraduate/licenciatura-pregrado/download");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ProximasLicenciatura.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(xml).Descendants(ns + "row").ToArray();
        Assert.Equal(5, rows[0].Descendants(ns + "t").Count());
    }

    [Fact]
    public async Task Download_without_rows_returns_400()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures { NoData = true }, tokens);
        using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE"));
        using var response = await client.GetAsync("/api/reports/postgraduate/licenciatura-pregrado/download");
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
            .UseInMemoryDatabase("licenciatura-" + Guid.NewGuid()).Options))
    {
        public bool NoData { get; set; }

        public override Task<List<SpListAlumnosCumpleLicenciaturaResult>?> SpList_AlumnosCumpleLicenciaturaAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            if (NoData) return Task.FromResult<List<SpListAlumnosCumpleLicenciaturaResult>?>([]);
            return Task.FromResult<List<SpListAlumnosCumpleLicenciaturaResult>?>([
                new() { Cod_Alumno = "A/1", Nombre = "Ana Pérez", NombreCarrera = "Ingeniería Comercial",
                    TituloGrado = "Licenciado en Ciencias", Estado = "Cumple" },
            ]);
        }
    }
}
