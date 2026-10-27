using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Application.Reports.Postgraduate.TeacherSurvey;

namespace WebApi.Tests;

public sealed class TeacherSurveyTests
{
    private const string Route = "/api/reports/postgraduate/teacher-survey";

    private static (WebApplicationFactory<Program> Factory, TokenFactory Tokens) Create(
        ITeacherSurveyReader reader, params string[] roles)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(reader);
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    private static HttpClient Authorized(WebApplicationFactory<Program> factory, TokenFactory tokens,
        params string[] roles) { var client = factory.CreateClient(); client.SetBearer(tokens.CreateToken("SADInformes", roles)); return client; }

    [Fact]
    public async Task Report_requires_authentication()
    {
        var (factory, _) = Create(new FakeTeacherSurveyReader());
        using var _factory = factory;
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Route + "/filters?tipoAlumno=MBA")).StatusCode);
    }

    [Fact]
    public async Task Entry_permission_alone_cannot_read_report_data()
    {
        var (factory, tokens) = Create(new FakeTeacherSurveyReader());
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_ACCESS");
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Authorized_request_returns_shared_envelope_rows_and_total()
    {
        var reader = new FakeTeacherSurveyReader();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetProperty("total").GetInt32());
        var row = data.GetProperty("rows")[0];
        Assert.Equal("2024A", row.GetProperty("period").GetString());
        Assert.Equal("MBA101/1", row.GetProperty("course").GetString());
        Assert.Equal("Gestión (MBA101)", row.GetProperty("name").GetString());
        Assert.Equal("Profesora Prueba", row.GetProperty("teacherName").GetString());
        Assert.Equal(4.5m, row.GetProperty("a1").GetDecimal());
        Assert.Equal(4m, row.GetProperty("b13").GetDecimal());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("bGrade").ValueKind);
        Assert.Equal("comentario B", row.GetProperty("bComments").GetString());
        Assert.Equal(("2024A", "MBA", "PT"), (reader.LastPeriod, reader.LastType, reader.LastModality));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?periodo=&tipoAlumno=MBA&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=MBA&modalidad=")]
    [InlineData("?periodo=Todos&tipoAlumno=MBA&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=Todos&modalidad=PT")]
    [InlineData("?periodo=2024A&tipoAlumno=MBA&modalidad=Todos")]
    [InlineData("?periodo=%25&tipoAlumno=MBA&modalidad=PT")]
    public async Task Invalid_filters_are_rejected_instead_of_returning_an_empty_table(string query)
    {
        var reader = new FakeTeacherSurveyReader();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(reader.LastPeriod);
    }

    [Fact]
    public async Task Filters_are_returned_for_the_selected_student_type()
    {
        var reader = new FakeTeacherSurveyReader();
        var (factory, tokens) = Create(reader);
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "/filters?tipoAlumno=MBA");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal("2024A", data.GetProperty("periods")[0].GetProperty("value").GetString());
        Assert.Equal("MBA", data.GetProperty("studentTypes")[1].GetProperty("value").GetString());
        Assert.Equal("PT", data.GetProperty("modalities")[0].GetString());
        Assert.Equal("MBA", reader.LastFilterType);
    }

    [Fact]
    public async Task Storage_failures_are_sanitized()
    {
        var (factory, tokens) = Create(new FakeTeacherSurveyReader { Failure = new InvalidOperationException("private-sql-detail") });
        using var _factory = factory;
        using var tokensScope = tokens;
        using var client = Authorized(factory, tokens, "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY");
        var response = await client.GetAsync(Route + "?periodo=2024A&tipoAlumno=MBA&modalidad=PT");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-sql-detail", body);
        Assert.Contains("no está disponible", body);
    }

    private sealed class FakeTeacherSurveyReader : ITeacherSurveyReader
    {
        public Exception? Failure { get; init; }
        public string? LastPeriod { get; private set; }
        public string? LastType { get; private set; }
        public string? LastModality { get; private set; }
        public string? LastFilterType { get; private set; }

        public Task<TeacherSurveyFilters> GetFiltersAsync(string? studentType, CancellationToken cancellationToken)
        {
            if (Failure is not null) return Task.FromException<TeacherSurveyFilters>(Failure);
            LastFilterType = studentType;
            return Task.FromResult(new TeacherSurveyFilters(
                [new("2024A", "2024 - Primer Semestre")],
                [new("MGP", "Magíster en Gestión Pública"), new("MBA", "MBA")],
                ["PT", "FT"]));
        }

        public Task<IReadOnlyList<TeacherSurveyRow>> GetRowsAsync(
            string period, string studentType, string modality, CancellationToken cancellationToken)
        {
            if (Failure is not null) return Task.FromException<IReadOnlyList<TeacherSurveyRow>>(Failure);
            LastPeriod = period;
            LastType = studentType;
            LastModality = modality;
            return Task.FromResult<IReadOnlyList<TeacherSurveyRow>>(
            [
                new(period, "MBA101/1", "Gestión (MBA101)", "12345678-9", "Profesora Prueba", "Ayudante Uno",
                    4.5, 4, 4, 4, "comentario A", 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                    null, "comentario B"),
            ]);
        }
    }
}
