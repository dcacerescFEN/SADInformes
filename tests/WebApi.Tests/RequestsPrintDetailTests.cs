using Application.Common.Interfaces;
using Application.Common.Reporting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace WebApi.Tests;

public sealed class RequestsPrintDetailTests
{
    private const string Route = "/api/reports/requests/by-status/print-detail";
    private const string Access = "PERMISSION_REPORT_ACCESS_REPORT_MODULE";
    private const string View = "PERMISSION_REPORT_VIEW_REQUESTS";
    private const string Print = "PERMISSION_REPORT_PRINT_REQUESTS";

    private static (WebApplicationFactory<Program>, TokenFactory) Create(Renderer renderer)
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
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    [Fact]
    public async Task Authentication_is_required()
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();

        using var response = await client.GetAsync(Route + "?requestCode=2024200001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(renderer.Request);
    }

    [Theory]
    [InlineData(Access)]
    [InlineData(View)]
    [InlineData(Access, View)]
    public async Task Print_permission_is_required(params string[] roles)
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", roles));

        using var response = await client.GetAsync(Route + "?requestCode=2024200001");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(renderer.Request);
    }

    [Fact]
    public async Task Detail_is_rendered_as_pdf_with_the_solicitud_impresion_report()
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View, Print));

        using var response = await client.GetAsync(
            Route + "?requestCode=" + Uri.EscapeDataString("  2024200001  "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SolicitudImpresion", renderer.Request!.ReportName);
        Assert.Equal(ReportFormat.PDF, renderer.Request.Format);
        Assert.Equal(TimeSpan.FromSeconds(120), renderer.Request.OperationTimeout);
        var parameter = Assert.Single(renderer.Request.Parameters);
        Assert.Equal("CodSolicitud", parameter.Key);
        Assert.Equal("2024200001", parameter.Value);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("Solicitud_2024200001.pdf",
            response.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        Assert.Equal("pdf-test", await response.Content.ReadAsStringAsync());
        Assert.True(renderer.Disposed);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Slash_in_the_database_code_is_removed_for_the_report_and_file_name()
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View, Print));

        using var response = await client.GetAsync(
            Route + "?requestCode=" + Uri.EscapeDataString("00773/20262EEYA"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var parameter = Assert.Single(renderer.Request!.Parameters);
        Assert.Equal("0077320262EEYA", parameter.Value);
        Assert.Equal("Solicitud_0077320262EEYA.pdf",
            response.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("202420000120242000001")]
    [InlineData("2024-00001")]
    [InlineData("2024 00001")]
    [InlineData("٢٠٢٤٢٠٠٠٠١")]
    public async Task Invalid_code_is_rejected_before_rendering(string code)
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View, Print));

        using var response = await client.GetAsync(
            Route + "?requestCode=" + Uri.EscapeDataString(code));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(renderer.Request);
    }

    [Fact]
    public async Task Renderer_failure_is_sanitized()
    {
        var renderer = new Renderer { Fail = true };
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, View, Print));

        using var response = await client.GetAsync(Route + "?requestCode=2024200001");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("El informe no está disponible.", body);
        Assert.DoesNotContain("No fue posible obtener el reporte solicitado.", body);
    }

    [Fact]
    public async Task Existing_status_endpoints_keep_their_authorization_and_validation()
    {
        var renderer = new Renderer();
        var (factory, tokens) = Create(renderer);
        using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", Access, Print));

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/reports/requests/by-status/report")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/reports/requests/by-status/types")).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", Access, View));
        using var invalid = await client.GetAsync("/api/reports/requests/by-status/types?career=XX");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Null(renderer.Request);
    }

    private sealed class Renderer : IReportRenderer, IReportRenderResult
    {
        public ReportRenderRequest? Request { get; private set; }
        public bool Disposed { get; private set; }
        public bool Fail { get; init; }
        public string ContentType => "application/pdf";
        public string FileExtension => "pdf";
        public Task<IReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct)
        { Request = request; if (Fail) throw new ReportRenderException(); return Task.FromResult<IReportRenderResult>(this); }
        public Task CopyToAsync(Stream stream, CancellationToken ct) =>
            stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("pdf-test"), ct).AsTask();
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
