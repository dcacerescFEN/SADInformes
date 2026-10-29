using Application.Common.Interfaces;
using Application.Common.Reporting;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace WebApi.Tests;

public sealed class StudentRequestsTests
{
    private const string Route = "/api/reports/postgraduate/student-requests";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_STUDENT_REQUESTS";
    private static (WebApplicationFactory<Program>, TokenFactory) Create(Renderer renderer, Procedures? procedures = null)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IReportRenderer>(renderer);
                services.AddSingleton<ISadContextProcedures>(procedures ?? new Procedures());
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", 403)]
    [InlineData("PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES", 403)]
    public async Task Permission_is_required_on_both_routes(string? role, int status)
    {
        var renderer = new Renderer();
        var procedures = new Procedures();
        var (factory, tokens) = Create(renderer, procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        foreach (var suffix in new[] { "", "/filters" })
            Assert.Equal(status, (int)(await client.GetAsync(Route + suffix)).StatusCode);
        Assert.Null(renderer.Request);
        Assert.Equal(0, procedures.Calls);
    }

    [Theory]
    [InlineData("PENDIENTE")]
    [InlineData("EN CURSO")]
    [InlineData("RESUELTA")]
    [InlineData("ANULADA")]
    public async Task Fixed_report_binds_selected_state_not_year_and_downloads_excel_by_default(string state)
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "?studentType=MBA&modality=ANTOFAGASTA&cohort=2026&state=" + Uri.EscapeDataString(state));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SolicitudesAlumnoPostgrado", renderer.Request!.ReportName);
        Assert.Equal(ReportFormat.EXCEL, renderer.Request.Format);
        Assert.Equal(4, renderer.Request.Parameters.Count);
        Assert.Equal("MBA", renderer.Request.Parameters["TipoAlumno"]);
        Assert.Equal("ANTOFAGASTA", renderer.Request.Parameters["Modalidad"]);
        Assert.Equal("2026", renderer.Request.Parameters["AñoDeIngreso"]);
        Assert.Equal(state, renderer.Request.Parameters["EstadoSolicitud"]);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("SolicitudesAlumnoPostgrado.xlsx", response.Content.Headers.ContentDisposition!.ToString());
        Assert.Equal("excel-test", await response.Content.ReadAsStringAsync());
        Assert.True(renderer.Disposed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?studentType=Todos&cohort=Todos&state=Todos&modality=")]
    [InlineData("?format=EXCEL")]
    public async Task All_filters_are_omitted_like_legacy(string query)
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Route + query)).StatusCode);
        Assert.Empty(renderer.Request!.Parameters);
    }

    [Theory]
    [InlineData("state=2026")]
    [InlineData("studentType=invalid")]
    [InlineData("cohort=2025")]
    [InlineData("studentType=MBA&modality=other")]
    [InlineData("format=HTML")]
    [InlineData("format=PDF")]
    [InlineData("format=WORD")]
    [InlineData("format=CSV")]
    public async Task Invalid_selection_never_renders(string query)
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + "?" + query)).StatusCode);
        Assert.Null(renderer.Request);
    }

    [Fact]
    public async Task Filters_preserve_all_options_and_legacy_catalog_calls()
    {
        var renderer = new Renderer(); var procedures = new Procedures();
        var (factory, tokens) = Create(renderer, procedures);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route + "/filters?studentType=MBA");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Todos", data.GetProperty("studentTypes")[0].GetProperty("value").GetString());
        Assert.Equal("Todos", data.GetProperty("cohorts")[0].GetString());
        Assert.Equal(new[] { "Todos", "PENDIENTE", "EN CURSO", "RESUELTA", "ANULADA" },
            data.GetProperty("states").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains("ANTOFAGASTA", data.GetProperty("modalities").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal("MBA", procedures.ModalityType);
        Assert.Null(renderer.Request);
    }

    [Fact]
    public async Task Upstream_error_is_sanitized()
    {
        var renderer = new Renderer { Fail = true };
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", View));
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("El informe no está disponible.", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("private", await response.Content.ReadAsStringAsync());
    }

    private sealed class Renderer : IReportRenderer, IReportRenderResult
    {
        public ReportRenderRequest? Request { get; private set; }
        public bool Disposed { get; private set; }
        public bool Fail { get; init; }
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public string FileExtension => "xlsx";
        public Task<IReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct)
        { Request = request; if (Fail) throw new ReportRenderException(); return Task.FromResult<IReportRenderResult>(this); }
        public Task CopyToAsync(Stream stream, CancellationToken ct) => stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("excel-test"), ct).AsTask();
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
    private sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public int Calls { get; private set; }
        public string? ModalityType { get; private set; }
        public override Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(string? incluirTodos, string? usuario,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        { Calls++; Assert.Equal("SI", incluirTodos); Assert.Equal("", usuario);
            return Task.FromResult<List<SpRecTiposAlumnoResult>?>([new() { Tipo = "Todos", Nombre = "Todos" }, new() { Tipo = "MBA", Nombre = "MBA" }]); }
        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(string? incluirTodos, string? tipoAlumno,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        { Calls++; Assert.Equal(("SI", "Todos"), (incluirTodos, tipoAlumno));
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([new() { Año = "Todos" }, new() { Año = "2026" }]); }
        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        { Calls++; ModalityType = tipo; return Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>(
            tipo == "MBA" ? [new() { Modalidad = "ANTOFAGASTA" }] : []); }
    }
}
