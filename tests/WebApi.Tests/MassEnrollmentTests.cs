using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Reports.Postgraduate.MassEnrollment.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class MassEnrollmentTests
{
    private const string Route = "/api/reports/postgraduate/mass-enrollment";
    private const string Query = "?studentType=MBA&modality=PT&cohort=2024&period=2024A&course=MBA00101/1/0";
    private const string View = "PERMISSION_REPORT_VIEW_POSTGRADUATE_MASS_ENROLLMENT";
    private const string Manage = "PERMISSION_REPORT_MANAGE_POSTGRADUATE_MASS_ENROLLMENT";
    private static readonly EnrollmentSelection Selection = new("MBA", "PT", "2024", "2024A", "MBA00101/1/0");

    private static (WebApplicationFactory<Program>, TokenFactory) Create(FakeEnrollmentData data)
    {
        var tokens = new TokenFactory();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IMassEnrollmentData>(data);
                tokens.Configure(services);
                tokens.UseClient(services, "SADInformes");
            });
        });
        return (factory, tokens);
    }

    private static HttpClient Client(WebApplicationFactory<Program> factory, TokenFactory tokens, params string[] roles)
    {
        var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", roles));
        return client;
    }

    [Fact]
    public async Task Catalogues_and_candidates_preserve_legacy_fields_without_Todos()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View);
        var filters = await client.GetAsync(Route + "/filters" + Query);
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        var json = await filters.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Todos", json);
        Assert.Contains("MBA00101/1/0", json);
        Assert.False(string.IsNullOrWhiteSpace(data.CatalogueUser));
        var response = await client.GetAsync(Route + Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = body.RootElement.GetProperty("data").GetProperty("rows");
        Assert.Equal(3, rows.GetArrayLength());
        Assert.Equal("Alumno Uno", rows[0].GetProperty("name").GetString());
        Assert.Equal("2024A", rows[0].GetProperty("entrySemester").GetString());
        Assert.Equal("REGULAR", rows[0].GetProperty("situation").GetString());
        Assert.Equal("D1", rows[0].GetProperty("decree").GetString());
        Assert.True(rows[2].GetProperty("enrolled").GetBoolean());
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Read_and_write_permissions_are_distinct_and_unauthenticated_requests_fail()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Route + Query)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Route + "/filters")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Route, new EnrollmentRequest(Selection, ["A1"], true))).StatusCode);
        foreach (var role in new[] { "PERMISSION_REPORT_ACCESS", "PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY" })
        {
            using var denied = Client(factory, tokens, role);
            Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync(Route + Query)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync(Route + "/filters")).StatusCode);
        }
        using var reader = Client(factory, tokens, View);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync(Route, new EnrollmentRequest(Selection, ["A1"], true))).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?studentType=Todos&modality=PT&cohort=2024&period=2024A&course=MBA00101/1/0")]
    [InlineData("?studentType=MBA&modality=PT&cohort=2024&period=2024A&course=forged/1/0")]
    [InlineData("?studentType=MBA&modality=FT&cohort=2024&period=2024A&course=MBA00101/1/0")]
    public async Task Invalid_or_forged_filters_are_rejected_without_writes(string query)
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + query)).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Theory]
    [InlineData("A1", false, HttpStatusCode.BadRequest)]
    [InlineData("", true, HttpStatusCode.BadRequest)]
    [InlineData("A1,A1", true, HttpStatusCode.BadRequest)]
    [InlineData("forged", true, HttpStatusCode.Conflict)]
    [InlineData("A3", true, HttpStatusCode.Conflict)]
    public async Task Invalid_selection_leaves_enrollments_and_write_counter_unchanged(string codes, bool confirmed, HttpStatusCode expected)
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var before = await client.GetStringAsync(Route + Query);
        var response = await client.PostAsJsonAsync(Route, new EnrollmentRequest(Selection, codes.Split(',', StringSplitOptions.RemoveEmptyEntries), confirmed));
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(before, await client.GetStringAsync(Route + Query));
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Confirmed_batch_enrolls_selected_students_and_replay_cannot_duplicate_them()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var request = new EnrollmentRequest(Selection, ["A1", "A2"], true);
        var response = await client.PostAsJsonAsync(Route, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetProperty("data").GetProperty("enrolled").GetInt32());
        Assert.Equal(2, data.Writes);
        Assert.Equal("usuario-prueba", data.AuditUser);
        Assert.False(string.IsNullOrWhiteSpace(data.AuditPc));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Route, request)).StatusCode);
        Assert.Equal(2, data.Writes);
        using var rows = JsonDocument.Parse(await client.GetStringAsync(Route + Query));
        Assert.All(rows.RootElement.GetProperty("data").GetProperty("rows").EnumerateArray(),
            row => Assert.True(row.GetProperty("enrolled").GetBoolean()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Partial_failure_or_silent_noop_rolls_back_the_entire_batch(bool throws)
    {
        var data = new FakeEnrollmentData { FailSecond = throws, SkipSecond = !throws };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var before = await client.GetStringAsync(Route + Query);
        var response = await client.PostAsJsonAsync(Route, new EnrollmentRequest(Selection, ["A1", "A2"], true));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("private-sql-detail", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await client.GetStringAsync(Route + Query));
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Enrollment_is_rechecked_even_when_legacy_candidate_status_is_stale()
    {
        var data = new FakeEnrollmentData { StaleStudent = "A1" };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync(Route, new EnrollmentRequest(Selection, ["A1"], true))).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Manage_permission_without_view_permission_cannot_call_the_write_endpoint()
    {
        var data = new FakeEnrollmentData();
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, Manage);
        var response = await client.PostAsJsonAsync(
            Route, new EnrollmentRequest(Selection, ["A1"], true));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task ENECO660_02_last_vacancy_keeps_students_readable_and_rejects_more_enrollments()
    {
        var data = new FakeEnrollmentData { Course = "ENECO660/02/0", ExhaustAfterFirst = true };
        var selection = Selection with { Course = data.Course };
        var query = Query.Replace("MBA00101/1/0", data.Course);
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Route + query)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync(Route, new EnrollmentRequest(selection, ["A1"], true))).StatusCode);
        using var filters = JsonDocument.Parse(await client.GetStringAsync(Route + "/filters" + query));
        Assert.Equal(0, filters.RootElement.GetProperty("data").GetProperty("courses").GetArrayLength());

        var response = await client.GetAsync(Route + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadAsStringAsync();
        using var report = JsonDocument.Parse(snapshot);
        var result = report.RootElement.GetProperty("data");
        Assert.Equal(3, result.GetProperty("total").GetInt32());
        Assert.False(result.GetProperty("canEnroll").GetBoolean());
        Assert.Equal("El curso ya no tiene vacantes.", result.GetProperty("availabilityMessage").GetString());
        Assert.True(result.GetProperty("rows")[0].GetProperty("enrolled").GetBoolean());
        Assert.False(result.GetProperty("rows")[1].GetProperty("enrolled").GetBoolean());
        Assert.Equal(snapshot, await client.GetStringAsync(Route + query));

        var rejected = await client.PostAsJsonAsync(Route, new EnrollmentRequest(selection, ["A2"], true));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Contains("El curso ya no tiene vacantes.", await rejected.Content.ReadAsStringAsync());
        Assert.Equal(1, data.Writes);
        Assert.Equal(snapshot, await client.GetStringAsync(Route + query));
    }

    [Fact]
    public async Task A_batch_cannot_consume_more_than_the_single_remaining_vacancy()
    {
        var data = new FakeEnrollmentData { Course = "ENECO660/02/0", ExhaustAfterFirst = true };
        var selection = Selection with { Course = data.Course };
        var query = Query.Replace("MBA00101/1/0", data.Course);
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        var before = await client.GetStringAsync(Route + query);
        var rejected = await client.PostAsJsonAsync(Route, new EnrollmentRequest(selection, ["A1", "A2"], true));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal(0, data.Writes);
        Assert.Equal(before, await client.GetStringAsync(Route + query));
    }

    [Fact]
    public async Task Exhausted_course_read_still_validates_course_identity_and_other_filters()
    {
        var data = new FakeEnrollmentData { NoVacancies = true };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View, Manage);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync(Route + Query.Replace("MBA00101/1/0", "forged/02/0"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync(Route + Query.Replace("studentType=MBA", "studentType=MGP"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync(Route + Query.Replace("period=2024A", "period=2025A"))).StatusCode);
        Assert.Equal(0, data.Writes);
    }

    [Theory]
    [InlineData("studentType=MBA", "studentType=MGP")]
    [InlineData("modality=PT", "modality=FT")]
    public async Task Exhausted_course_from_another_valid_student_scope_is_rejected(string original, string otherScope)
    {
        var data = new FakeEnrollmentData
        {
            NoVacancies = true,
            StudentTypes = [new("MBA", "MBA"), new("MGP", "MGP")],
            Modalities = ["PT", "FT"],
        };
        var (factory, tokens) = Create(data);
        using var f = factory; using var t = tokens;
        using var client = Client(factory, tokens, View);
        var response = await client.GetAsync(Route + Query.Replace(original, otherScope));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Los filtros seleccionados no están disponibles.", body);
        Assert.DoesNotContain("Alumno Uno", body);
        Assert.Equal(0, data.CandidateReads);
        Assert.Equal(0, data.Writes);
    }

    private sealed class FakeEnrollmentData : IMassEnrollmentData
    {
        private HashSet<string> enrolled = ["A3"];
        public string Course { get; init; } = "MBA00101/1/0";
        public bool ExhaustAfterFirst { get; init; }
        public bool NoVacancies { get; init; }
        public IReadOnlyList<EnrollmentOption> StudentTypes { get; init; } = [new("MBA", "MBA")];
        public IReadOnlyList<string> Modalities { get; init; } = ["PT"];
        public int CandidateReads { get; private set; }
        public bool FailSecond { get; init; }
        public bool SkipSecond { get; init; }
        public string? StaleStudent { get; init; }
        public int Writes { get; private set; }
        public string? AuditUser { get; private set; }
        public string? AuditPc { get; private set; }
        public string? CatalogueUser { get; private set; }

        public Task<MassEnrollmentFilters> GetFiltersAsync(EnrollmentSelection selection, string user, CancellationToken ct)
        {
            CatalogueUser = user;
            return Task.FromResult(new MassEnrollmentFilters(StudentTypes, Modalities, ["2024"],
                [new("2024A", "Primer semestre")], NoVacancies || (ExhaustAfterFirst && Writes > 0) ? [] : [new(Course, "Curso Uno")]));
        }

        public Task<EnrollmentOption?> FindCourseAsync(EnrollmentSelection selection, CancellationToken ct) =>
            Task.FromResult<EnrollmentOption?>(selection.Course == Course && selection.StudentType == "MBA" && selection.Modality == "PT"
                ? new(Course, "Curso Uno") : null);

        public Task<IReadOnlyList<MassEnrollmentRow>> GetCandidatesAsync(EnrollmentSelection selection, CancellationToken ct)
        {
            CandidateReads++;
            return Task.FromResult<IReadOnlyList<MassEnrollmentRow>>(
                new[] { "A1", "A2", "A3" }.Select((id, i) => new MassEnrollmentRow(id,
                    i == 0 ? "Alumno Uno" : id, "12345678-9", "2024A", "REGULAR", "", "D1", enrolled.Contains(id))).ToArray());
        }

        public Task<bool> IsEnrolledAsync(string student, EnrollmentSelection selection, CancellationToken ct) =>
            Task.FromResult(enrolled.Contains(student) || StaleStudent == student);

        public Task AddAsync(string student, EnrollmentSelection selection, string user, string pc, CancellationToken ct)
        {
            AuditUser = user; AuditPc = pc;
            if (student == "A2" && FailSecond) throw new InvalidOperationException("private-sql-detail");
            if (!(student == "A2" && SkipSecond)) { enrolled.Add(student); Writes++; }
            return Task.CompletedTask;
        }

        public Task<IEnrollmentTransaction> BeginAsync(CancellationToken ct) =>
            Task.FromResult<IEnrollmentTransaction>(new Transaction(this));

        private sealed class Transaction(FakeEnrollmentData data) : IEnrollmentTransaction
        {
            private readonly HashSet<string> snapshot = new(data.enrolled);
            private readonly int writes = data.Writes;
            private bool committed;
            public Task CommitAsync(CancellationToken ct) { committed = true; return Task.CompletedTask; }
            public ValueTask DisposeAsync()
            {
                if (!committed) { data.enrolled = snapshot; data.Writes = writes; }
                return ValueTask.CompletedTask;
            }
        }
    }
}
