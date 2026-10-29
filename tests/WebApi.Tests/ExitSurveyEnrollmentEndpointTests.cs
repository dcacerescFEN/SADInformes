using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Commands.CreateExitSurveyEnrollment;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class ExitSurveyEnrollmentEndpointTests
{
    private const string Route = "/api/reports/postgraduate/exit-survey-enrollment";
    private const string Query = "?studentType=Todos&modality=Todos&year=2024";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_ENROLLMENT";
    private const string Manage = "PERMISSION_REPORT_MANAGE_POSTGRADUATE_EXIT_SURVEY_ENROLLMENT";
    private static (WebApplicationFactory<Program>, TokenFactory) Create(ExitSurveyEnrollmentTests.FakeData data)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContext>(data);
                services.AddSingleton<ISadContextProcedures>(new ExitSurveyEnrollmentTests.Procedures(data));
                services.AddSingleton<IPostgraduateSettings>(new ExitSurveyEnrollmentTests.Settings());
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }
    private static HttpClient Client(WebApplicationFactory<Program> factory, TokenFactory tokens, params string[] roles)
    {
        var client = factory.CreateClient(); client.SetBearer(tokens.CreateToken("SADInformes", roles)); return client;
    }

    [Theory]
    [InlineData(null, "/filters", 401)]
    [InlineData(null, "", 401)]
    [InlineData("PERMISSION_REPORT_ACCESS", "/filters", 403)]
    [InlineData("PERMISSION_REPORT_ACCESS", "", 403)]
    public async Task Listing_requires_its_specific_view_permission(string? role, string suffix, int status)
    {
        var (factory, tokens) = Create(new()); using var host = factory; using var signing = tokens; using var client = factory.CreateClient();
        if (role is not null) client.SetBearer(tokens.CreateToken("SADInformes", role));
        using var response = await client.GetAsync(Route + suffix + (suffix == "" ? Query : ""));
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Fact]
    public async Task View_can_read_but_cannot_enroll_and_manage_requires_view_as_well()
    {
        var data = new ExitSurveyEnrollmentTests.FakeData();
        var (factory, tokens) = Create(data); using var host = factory; using var signing = tokens;
        using var viewOnly = Client(factory, tokens, View);
        var viewList = await viewOnly.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, viewList.StatusCode);
        using (var json = JsonDocument.Parse(await viewList.Content.ReadAsStringAsync()))
            Assert.False(json.RootElement.GetProperty("data").GetProperty("canEnroll").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await viewOnly.PostAsJsonAsync(Route,
            new CreateExitSurveyEnrollmentRequestDTO("Todos", "Todos", "2024", ["100-20241"], true))).StatusCode);
        using var manageOnly = Client(factory, tokens, Manage);
        Assert.Equal(HttpStatusCode.Forbidden, (await manageOnly.GetAsync(Route + Query)).StatusCode);
        Assert.Empty(data.InsertedCodes);
    }

    [Theory]
    [InlineData(View, Manage)]
    [InlineData("ROLE_REPORT_ADMIN")]
    [InlineData("ROLE_REPORT_SUPERADMIN")]
    public async Task Authorized_roles_receive_the_legacy_listing_and_enroll_only_new_rows(params string[] roles)
    {
        var data = new ExitSurveyEnrollmentTests.FakeData();
        data.EncuestaSalidaGraduados.Add(new() { Folio = 1, CodAlumno = "200-20241" }); await data.SaveChangesAsync();
        var (factory, tokens) = Create(data); using var host = factory; using var signing = tokens; using var client = Client(factory, tokens, roles);
        using var filters = await client.GetAsync(Route + "/filters"); Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.Contains("Todos", await filters.Content.ReadAsStringAsync());
        using var list = await client.GetAsync(Route + Query); Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.True(list.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("canEnroll").GetBoolean());
        var rows = json.RootElement.GetProperty("data").GetProperty("rows");
        Assert.Equal(2, rows.GetArrayLength());
        Assert.Equal("P1", rows[0].GetProperty("personCode").GetString());
        Assert.Equal("15-12-2023", rows[0].GetProperty("exitDate").GetString());
        Assert.True(rows[0].GetProperty("enrolled").GetBoolean());
        Assert.Equal("40%", rows[0].GetProperty("progress").GetString());
        var response = await client.PostAsJsonAsync(Route, new CreateExitSurveyEnrollmentRequestDTO("Todos", "Todos", "2024", ["100-20241", "200-20241"], true));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, result.RootElement.GetProperty("data").GetProperty("enrolled").GetInt32());
        Assert.Equal(1, result.RootElement.GetProperty("data").GetProperty("skipped").GetInt32());
        Assert.Equal(new[] { "100-20241" }, data.InsertedCodes);
        Assert.Equal(1, data.Commits);
    }

    [Theory]
    [InlineData("Todos", "Todos", "2024", "999-20241", 409)]
    [InlineData("X", "Todos", "2024", "100-20241", 400)]
    [InlineData("Todos", "Todos", "2024A", "100-20241", 400)]
    [InlineData("Todos", "Todos", "2024", "", 400)]
    public async Task Stale_or_invalid_selection_never_writes(string type, string mode, string year, string codes, int status)
    {
        var data = new ExitSurveyEnrollmentTests.FakeData();
        var (factory, tokens) = Create(data); using var host = factory; using var signing = tokens; using var client = Client(factory, tokens, View, Manage);
        var response = await client.PostAsJsonAsync(Route, new CreateExitSurveyEnrollmentRequestDTO(type, mode, year,
            codes.Split(',', StringSplitOptions.RemoveEmptyEntries), true));
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Empty(data.InsertedCodes); Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Read_and_write_storage_failures_use_their_own_sanitized_message()
    {
        var data = new ExitSurveyEnrollmentTests.FakeData { FailReads = true };
        var (factory, tokens) = Create(data); using var host = factory; using var signing = tokens; using var client = Client(factory, tokens, View, Manage);
        foreach (var suffix in new[] { "/filters", "" })
        {
            var read = await client.GetAsync(Route + suffix + (suffix == "" ? Query : ""));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, read.StatusCode);
            var readBody = await read.Content.ReadAsStringAsync();
            Assert.Contains("El informe no está disponible.", readBody); Assert.DoesNotContain("private-read", readBody);
        }
        var write = await client.PostAsJsonAsync(Route, new CreateExitSurveyEnrollmentRequestDTO("Todos", "Todos", "2024", ["100-20241"], true));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, write.StatusCode);
        Assert.Contains("No se pudo confirmar la inscripción", await write.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Missing_confirmation_and_procedure_failure_are_sanitized_and_fully_rolled_back()
    {
        var data = new ExitSurveyEnrollmentTests.FakeData();
        var (factory, tokens) = Create(data); using var host = factory; using var signing = tokens; using var client = Client(factory, tokens, View, Manage);
        var missing = await client.PostAsJsonAsync(Route, new CreateExitSurveyEnrollmentRequestDTO("Todos", "Todos", "2024", ["100-20241"], false));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode); Assert.Contains("Debe confirmar", await missing.Content.ReadAsStringAsync());
        data.FailNext = true;
        var failed = await client.PostAsJsonAsync(Route, new CreateExitSurveyEnrollmentRequestDTO("Todos", "Todos", "2024", ["100-20241"], true));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        var body = await failed.Content.ReadAsStringAsync(); Assert.DoesNotContain("private-sql", body); Assert.Contains("No se pudo confirmar", body);
        Assert.Equal(0, data.EncuestaSalidaGraduados.Count()); Assert.Equal(1, data.Rollbacks);
    }
}
