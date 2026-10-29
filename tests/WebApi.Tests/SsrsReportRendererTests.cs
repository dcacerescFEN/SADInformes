using System.Net;
using Application.Common.Reporting;
using Infrastructure.Configuration;
using Infrastructure.Reporting;
using Xunit;

namespace WebApi.Tests;

public sealed class SsrsReportRendererTests
{
    private static ReportingSettings Settings() => new(new Dictionary<string, string?>
    {
        ["ReportingServiceBaseUrl"] = "https://reports.invalid/ReportServer",
        ["ReportingServicePath"] = "Reportes/SADInformes",
        ["ReportingServiceDomain"] = "test-domain",
        ["ReportingServiceUsername"] = "test-user",
        ["ReportingServicePassword"] = "test-password",
    });
    private static ReportRenderRequest Request(ReportFormat format = ReportFormat.PDF) => new(
        "SolicitudesAlumnoPostgrado", new Dictionary<string, string> { ["Modalidad"] = "á &+=?#/" },
        format, TimeSpan.FromSeconds(10));

    [Theory]
    [InlineData(ReportFormat.PDF, "PDF", "application/pdf", "pdf")]
    [InlineData(ReportFormat.EXCEL, "EXCELOPENXML", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx")]
    [InlineData(ReportFormat.WORD, "WORDOPENXML", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx")]
    [InlineData(ReportFormat.CSV, "CSV", "text/csv", "csv")]
    public async Task Reference_transport_preserves_formats_parameters_and_owned_stream(
        ReportFormat format, string ssrs, string mime, string extension)
    {
        using var handler = new Handler();
        await using var result = await new SsrsReportRenderer(new Factory(handler), Settings()).RenderAsync(Request(format), default);
        using var destination = new MemoryStream();
        await result.CopyToAsync(destination, default);
        Assert.Equal(new byte[] { 1, 2, 3 }, destination.ToArray());
        Assert.Equal(mime, result.ContentType);
        Assert.Equal(extension, result.FileExtension);
        Assert.Equal("https://reports.invalid/ReportServer?/Reportes/SADInformes/SolicitudesAlumnoPostgrado&rs:Command=Render&rs:Format="
            + ssrs + "&Modalidad=%C3%A1%20%26%2B%3D%3F%23%2F", handler.Url);
        await Assert.ThrowsAsync<InvalidOperationException>(() => result.CopyToAsync(destination, default));
        await result.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => result.CopyToAsync(destination, default));
    }

    [Theory]
    [InlineData("rs:Format")]
    [InlineData(" RC:Toolbar")]
    [InlineData("bad\nkey")]
    public async Task Reserved_parameter_injection_never_sends(string key)
    {
        using var handler = new Handler();
        await Assert.ThrowsAsync<ArgumentException>(() => new SsrsReportRenderer(new Factory(handler), Settings()).RenderAsync(
            Request() with { Parameters = new Dictionary<string, string> { [key] = "private" } }, default));
        Assert.Null(handler.Url);
    }

    [Theory]
    [InlineData("../Other")]
    [InlineData("https://other.invalid")]
    [InlineData("%2e%2e")]
    public async Task Invalid_report_paths_never_send(string report)
    {
        using var handler = new Handler();
        await Assert.ThrowsAsync<ArgumentException>(() => new SsrsReportRenderer(new Factory(handler), Settings()).RenderAsync(
            Request() with { ReportName = report }, default));
        Assert.Null(handler.Url);
    }

    [Fact]
    public async Task Upstream_failures_are_sanitized()
    {
        using var handler = new Handler { Status = HttpStatusCode.InternalServerError };
        var error = await Assert.ThrowsAsync<ReportRenderException>(() =>
            new SsrsReportRenderer(new Factory(handler), Settings()).RenderAsync(Request(), default));
        Assert.Equal(500, error.StatusCode);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain("private", error.ToString());
    }

    [Fact]
    public async Task Missing_reporting_keys_render_using_reference_server_and_SADInformes_folder()
    {
        using var handler = new Handler();
        await using var result = await new SsrsReportRenderer(new Factory(handler),
            new ReportingSettings(new Dictionary<string, string?>())).RenderAsync(Request(), default);
        Assert.StartsWith("https://reportes-pbi-dev2.fen.uchile.cl/ReportServer?/Reportes/Desarrollo/SADInformes/SolicitudesAlumnoPostgrado&", handler.Url);
        Assert.Equal("pdf", result.FileExtension);
    }

    [Fact]
    public void Default_constructor_initializes_reporting_even_before_AppConfig_is_available()
    {
        var settings = new ReportingSettings();
        Assert.Equal("https://reportes-pbi-dev2.fen.uchile.cl/ReportServer", settings.BaseUrl);
        Assert.Equal("Reportes/Desarrollo/SADInformes", settings.Path);
    }

    [Theory]
    [InlineData("ReportingServiceBaseUrl", " ")]
    [InlineData("ReportingServicePath", " ")]
    [InlineData("ReportingServiceBaseUrl", "file:///private")]
    [InlineData("ReportingServicePath", "../other")]
    public async Task Explicit_invalid_destination_fails_closed_without_transport(string key, string value)
    {
        using var handler = new Handler();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SsrsReportRenderer(new Factory(handler),
            new ReportingSettings(new Dictionary<string, string?> { [key] = value })).RenderAsync(Request(), default));
        Assert.Null(handler.Url);
    }

    [Fact]
    public async Task Already_cancelled_requests_do_not_send()
    {
        using var handler = new Handler();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SsrsReportRenderer(new Factory(handler), Settings())
            .RenderAsync(Request(), cancellation.Token));
        Assert.Null(handler.Url);
    }

    [Fact]
    public void Missing_credentials_use_the_same_defaults_as_SADAlumnos()
    {
        var settings = new ReportingSettings(new Dictionary<string, string?>
        {
            ["ReportingServiceBaseUrl"] = "https://reports.invalid/ReportServer",
            ["ReportingServicePath"] = "Reportes/SADInformes",
        });
        Assert.Equal("dfen", settings.Domain);
        Assert.Equal("reportes", settings.Username);
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(settings.Password))).ToLowerInvariant();
        Assert.True(digest == "ce60b239b10dd6416b1ebdb8f6324cc3f0a05abe6c5f07d22997106ecba0960f",
            "The fallback must match the reference without disclosing its value.");
    }

    [Fact]
    public void Explicit_credentials_override_defaults_without_trimming_password()
    {
        var settings = Settings();
        Assert.Equal("test-domain", settings.Domain);
        Assert.Equal("test-user", settings.Username);
        Assert.Equal("test-password", settings.Password);
        var spaced = new ReportingSettings(new Dictionary<string, string?>
        {
            ["ReportingServiceBaseUrl"] = "https://reports.invalid/ReportServer",
            ["ReportingServicePath"] = "Reportes/SADInformes",
            ["ReportingServicePassword"] = " test-password ",
        });
        Assert.Equal(" test-password ", spaced.Password);
    }

    [Theory]
    [InlineData("ReportingServiceUsername")]
    [InlineData("ReportingServicePassword")]
    [InlineData("ReportingServiceDomain")]
    public void Explicit_blank_credentials_do_not_silently_fall_back(string key)
    {
        var settings = new ReportingSettings(new Dictionary<string, string?>
        {
            ["ReportingServiceBaseUrl"] = "https://reports.invalid/ReportServer",
            ["ReportingServicePath"] = "Reportes/SADInformes",
            [key] = " ",
        });
        Assert.Throws<InvalidOperationException>(() => settings.Password);
    }

    private sealed class Handler : HttpMessageHandler
    {
        public string? Url { get; private set; }
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Url = request.RequestUri!.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new ByteArrayContent([1, 2, 3]), ReasonPhrase = "private" });
        }
    }
    private sealed class Factory(Handler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }
}
