using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class EnrolledStudentsEndpointTests
{
    private const string Route = "/api/reports/postgraduate/enrolled-students";
    private const string Query = "?modality=SUNRISE&year=2024";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_ENROLLED_STUDENTS";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(EnrolledStudentsTests.Procedures procedures)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    [Fact]
    public async Task Authenticated_filter_catalog_cannot_be_cached()
    {
        var (factory, tokens) = Create(new EnrolledStudentsTests.Procedures());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/filters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var modalities = json.RootElement.GetProperty("data").GetProperty("modalities");
        Assert.Equal(new[] { "FOR THE AMERICAS", "SUNRISE" },
            modalities.EnumerateArray().Select(row => row.GetProperty("value").GetString()));
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES", 403)]
    public async Task All_routes_require_the_enrolled_students_permission(string? role, int status)
    {
        var procedures = new EnrolledStudentsTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        Assert.Equal(status, (int)(await client.GetAsync(Route + "/filters")).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync(Route + "/download" + Query)).StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Download_has_the_fifteen_legacy_columns_and_preserves_text()
    {
        var (factory, tokens) = Create(new EnrolledStudentsTests.Procedures());
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("AlumnosMatriculados.xlsx",
            response.Content.Headers.ContentDisposition!.ToString());
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(xml);
        var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(new[]
        {
            "Cod Alumno", "Semestre_Ingreso", "Cod_Persona", "Tipo_Alumno", "Modalidad",
            "Situacion_Academica", "Nombres", "Apellidos", "Email_Personal", "Email_Fen",
            "Email_laboral", "Celular", "Tel_laboral", "NomEmpresa", "CargoEmpresa",
        }, rows[0].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Equal(new[]
        {
            "A/1", "2024F", "00123456-K", "MBA", "SUNRISE", "REGULAR", "Ana", "Pérez",
            "ana@example.test", "ana@fen.uchile.cl", "ana@work.test", "+56911111111",
            "+56222222222", "Empresa", "Cargo",
        }, rows[1].Descendants(ns + "t").Select(cell => cell.Value));
        Assert.Empty(sheet.Descendants(ns + "f"));
        Assert.All(sheet.Descendants(ns + "c"), cell => Assert.Equal("inlineStr", cell.Attribute("t")!.Value));
        Assert.All(rows, row => Assert.Equal(15, row.Elements(ns + "c").Count()));
    }

    [Fact]
    public async Task Invalid_selection_fails_without_calling_the_procedure()
    {
        var procedures = new EnrolledStudentsTests.Procedures();
        var (factory, tokens) = Create(procedures);
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download?modality=Todos&year=2024");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_result_has_no_attachment()
    {
        var (factory, tokens) = Create(new EnrolledStudentsTests.Procedures { Empty = true });
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Sin Datos", await response.Content.ReadAsStringAsync());
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Procedure_errors_are_sanitized()
    {
        var (factory, tokens) = Create(new EnrolledStudentsTests.Procedures { Fail = true });
        using var host = factory;
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/download" + Query);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("private-sql", body);
    }
}
