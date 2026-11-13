using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhoto;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoPage;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Photos;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class StudentsWithoutPhotoTests
{
    [Fact]
    public async Task Query_keeps_only_people_without_a_photo()
    {
        var response = await new GetStudentsWithoutPhotoHandler(
            new Procedures(), new Probe(["P2"]), new ApiResponseService())
            .Handle(new GetStudentsWithoutPhoto(), default);

        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("ANA PÉREZ", "P2", "A/2", "IC", "912345678", "aperez@fen.uchile.cl", "ana@test.cl"),
            (row.Name, row.Rut, row.StudentCode, row.StudentType, row.Mobile, row.MailFen, row.PersonalMail));
        Assert.Equal(1, response.Data.Total);
    }

    [Fact]
    public async Task Query_preserves_order_and_skips_the_probe_for_blank_person_code()
    {
        var probe = new Probe(["P1", "P5"]);
        var rows = new[]
        {
            Row("P1", "s1"), Row("", "s2"), Row("P3", "s3"), Row("P4", "s4"), Row("P5", "s5"),
        };
        var response = await new GetStudentsWithoutPhotoHandler(
            new Procedures(rows), probe, new ApiResponseService())
            .Handle(new GetStudentsWithoutPhoto(), default);

        Assert.Equal(["s1", "s2", "s5"], response.Data.Rows.Select(row => row.StudentCode));
        Assert.Equal(3, response.Data.Total);
        Assert.DoesNotContain("", probe.Codes);
        Assert.Equal(4, probe.Codes.Count);
    }

    [Fact]
    public async Task Concurrency_is_bounded_to_eight()
    {
        var rows = Enumerable.Range(1, 40)
            .Select(index => Row("P" + index, "s" + index)).ToArray();
        var probe = new ConcurrencyProbe();
        await new GetStudentsWithoutPhotoHandler(
            new Procedures(rows), probe, new ApiResponseService())
            .Handle(new GetStudentsWithoutPhoto(), default);

        Assert.Equal(40, probe.Calls);
        Assert.InRange(probe.Max, 2, 8);
    }

    [Fact]
    public async Task Page_returns_missing_rows_and_the_next_offset()
    {
        var rows = new[] { Row("P1", "s1"), Row("P2", "s2"), Row("P3", "s3"), Row("P4", "s4"), Row("P5", "s5") };
        var probe = new Probe(["P1", "P2", "P3", "P5"]);
        var first = await new GetStudentsWithoutPhotoPageHandler(
            new Procedures(rows), probe, new ApiResponseService())
            .Handle(new GetStudentsWithoutPhotoPage(1, 2, null), default);

        Assert.Equal(["s1", "s2"], first.Data.Rows.Select(row => row.StudentCode));
        Assert.Equal(1, first.Data.Page);
        Assert.True(first.Data.HasMore);

        var second = await new GetStudentsWithoutPhotoPageHandler(
            new Procedures(rows), probe, new ApiResponseService())
            .Handle(new GetStudentsWithoutPhotoPage(2, 2, null), default);

        Assert.Equal(["s3", "s5"], second.Data.Rows.Select(row => row.StudentCode));
        Assert.Equal(2, second.Data.Page);
        Assert.False(second.Data.HasMore);
    }

    [Fact]
    public async Task Page_does_not_stop_at_the_first_500_candidates()
    {
        var rows = Enumerable.Range(0, 520).Select(index => Row("P" + index, "s" + index)).ToArray();
        var missing = Enumerable.Range(490, 30).Select(index => "P" + index).ToArray();
        var probe = new Probe(missing);

        var first = await new GetStudentsWithoutPhotoPageHandler(
            new Procedures(rows), probe, new ApiResponseService())
            .Handle(new GetStudentsWithoutPhotoPage(1, 10, null), default);
        Assert.Equal(10, first.Data.Rows.Length);
        Assert.True(first.Data.HasMore);

        var second = await new GetStudentsWithoutPhotoPageHandler(
            new Procedures(rows), probe, new ApiResponseService())
            .Handle(new GetStudentsWithoutPhotoPage(2, 10, null), default);
        Assert.Equal(10, second.Data.Rows.Length);
        Assert.Equal("s500", second.Data.Rows[0].StudentCode);
    }

    [Fact]
    public async Task Page_search_filters_candidates_before_probing()
    {
        var probe = new Probe(["P2"]);
        var response = await new GetStudentsWithoutPhotoPageHandler(
            new Procedures(), probe, new ApiResponseService())
            .Handle(new GetStudentsWithoutPhotoPage(1, 10, "ANA"), default);

        Assert.Equal("A/2", Assert.Single(response.Data.Rows).StudentCode);
        Assert.Equal(["P2"], probe.Codes);
        Assert.Equal(1, response.Data.Scanned);
    }

    [Fact]
    public async Task Endpoints_require_report_access_and_return_rows()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe(["P2"]), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal((int)HttpStatusCode.Unauthorized,
            (int)(await client.GetAsync(ReportUrl)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<StudentsWithoutPhotoPageDTO>>();
        Assert.Equal("P2", Assert.Single(body!.Data.Rows).Rut);
    }

    [Fact]
    public async Task Export_returns_xlsx_with_the_seven_headers()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe(["P2"]), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(ExportUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("SinFotos.xlsx", response.Content.Headers.ContentDisposition?.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var archive = new System.IO.Compression.ZipArchive(new System.IO.MemoryStream(bytes));
        using var sheet = new System.IO.StreamReader(
            archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = sheet.ReadToEnd();
        foreach (var header in new[]
        {
            "Nombre", "Rut", "Cód. Alumno", "Tipo Alumno", "Celular", "Mail Fen", "Mail Personal",
        })
            Assert.Contains(header, xml);
    }

    [Fact]
    public async Task Probe_treats_only_a_200_as_an_existing_photo()
    {
        using var missing = new HttpClient(new StubHandler(HttpStatusCode.NotFound));
        Assert.False(await CreateProbe(missing)
            .HasPhotoAsync("P1", default));

        using var present = new HttpClient(new StubHandler(HttpStatusCode.OK));
        Assert.True(await CreateProbe(present)
            .HasPhotoAsync("P1", default));

        using var failing = new HttpClient(new ThrowingHandler());
        Assert.False(await CreateProbe(failing)
            .HasPhotoAsync("P1", default));

        foreach (var status in new[]
        {
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NoContent,
        })
        {
            using var client = new HttpClient(new StubHandler(status));
            Assert.False(await CreateProbe(client)
                .HasPhotoAsync("P1", default), status.ToString());
        }

        using var slow = new HttpClient(new SlowHandler()) { Timeout = TimeSpan.FromMilliseconds(50) };
        Assert.False(await CreateProbe(slow)
            .HasPhotoAsync("P1", default));
    }

    [Fact]
    public async Task Export_without_rows_returns_400()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures([]), new Probe([]), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));

        using var response = await client.GetAsync(ExportUrl);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentDisposition);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Report_requires_the_report_permission()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe([]), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_USERS_READ"));

        using var response = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private const string ReportUrl =
        "/api/reports/academic-enrollment/students-without-photo/report?page=1&pageSize=10";
    private const string ExportUrl =
        "/api/reports/academic-enrollment/students-without-photo/export";

    private static WebApplicationFactory<Program> CreateFactory(
        Procedures procedures, Probe probe, TokenFactory tokens) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                services.AddSingleton<IStudentPhotoProbe>(probe);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });

    internal sealed class Probe(IReadOnlyCollection<string> missing) : IStudentPhotoProbe
    {
        public List<string> Codes { get; } = [];

        public Task<bool> HasPhotoAsync(string personCode, CancellationToken cancellationToken)
        {
            lock (Codes) Codes.Add(personCode);
            return Task.FromResult(!missing.Contains(personCode));
        }
    }

    internal sealed class ConcurrencyProbe : IStudentPhotoProbe
    {
        private int _current;
        private int _max;
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public int Max => Volatile.Read(ref _max);

        public async Task<bool> HasPhotoAsync(string personCode, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            var now = Interlocked.Increment(ref _current);
            int seen;
            while (now > (seen = Volatile.Read(ref _max)))
                Interlocked.CompareExchange(ref _max, now, seen);
            await Task.Delay(10, cancellationToken);
            Interlocked.Decrement(ref _current);
            return true;
        }
    }

    internal sealed class Settings : IStudentPhotoSettings
    {
        public string BaseUrl => "https://photos.test/";
    }

    private static HttpStudentPhotoProbe CreateProbe(HttpClient client) =>
        new(new Settings(), client, new MemoryCache(new MemoryCacheOptions()));

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("photo service unavailable");
    }

    private static SpRecPersonasSinFotosResult Row(string personCode, string studentCode) => new()
    {
        Cod_Persona = personCode, Cod_Alumno = studentCode, Nombre = "ALUMNO " + studentCode,
        Tipo_Alumno = "IC", Celular = "912345678", MailFen = "alumno@fen.uchile.cl",
        E_Mail = "alumno@test.cl",
    };

    private sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    internal sealed class Procedures(IReadOnlyList<SpRecPersonasSinFotosResult>? rows = null)
        : SadContextProcedures(
            new SadContext(new DbContextOptionsBuilder<SadContext>()
                .UseInMemoryDatabase("students-without-photo-" + Guid.NewGuid()).Options))
    {
        private readonly IReadOnlyList<SpRecPersonasSinFotosResult> _rows = rows ??
        [
            new()
            {
                Cod_Persona = "P1", Cod_Alumno = "A/1", Nombre = "LUIS SOTO", Tipo_Alumno = "IC",
                Celular = "900000000", MailFen = "lsoto@fen.uchile.cl", E_Mail = "luis@test.cl",
            },
            new()
            {
                Cod_Persona = "P2", Cod_Alumno = "A/2", Nombre = "ANA PÉREZ", Tipo_Alumno = "IC",
                Celular = "912345678", MailFen = "aperez@fen.uchile.cl", E_Mail = "ana@test.cl",
            },
        ];

        public override Task<List<SpRecPersonasSinFotosResult>?>
            SpRec_SAD_SAD_PersonasSinFotosAsync(
                OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPersonasSinFotosResult>?>(_rows.ToList());
    }
}
