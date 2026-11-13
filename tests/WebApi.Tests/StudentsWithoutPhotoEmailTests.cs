using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Commands.StartStudentsWithoutPhotoEmail;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailAvailability;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailPreview;
using Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries.GetStudentsWithoutPhotoEmailStatus;
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

[CollectionDefinition("EmailDispatch", DisableParallelization = true)]
public sealed class EmailDispatchCollection;

[Collection("EmailDispatch")]
public sealed class StudentsWithoutPhotoEmailTests
{
    [Fact]
    public async Task Preview_requires_report_access_and_returns_the_legacy_template()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe(), new Sender(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(PreviewUrl)).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        var preview = await client.GetFromJsonAsync<ApiResponse<EmailPreviewDTO>>(PreviewUrl);
        Assert.Equal("Fotografía Web de Docencia", preview!.Data.Subject);
        Assert.Contains("SECRETARIA DE ESTUDIOS", preview.Data.Body);
    }

    [Fact]
    public async Task Start_runs_in_background_and_uses_the_legacy_recipient_rules()
    {
        var sender = new Sender();
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe(), sender, tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        using var warmup = await client.GetAsync(PreviewUrl);
        EmailDeliverySettings.Initialize(true, true, "test@fen.uchile.cl", "secretariaestudios@fen.uchile.cl");

        using var response = await client.PostAsJsonAsync(EmailUrl, new { });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<ApiResponse<EmailDispatchStartedDTO>>();

        EmailDispatchSnapshot? status = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(100);
            status = (await client.GetFromJsonAsync<ApiResponse<EmailDispatchSnapshot>>(
                EmailUrl + "/" + started!.Data.JobId))!.Data;
            if (status.State is "COMPLETED" or "PARTIAL" or "FAILED") break;
        }

        Assert.Equal("COMPLETED", status!.State);
        Assert.Equal(2, status.Total);
        Assert.Equal(2, status.Sent);
        Assert.Equal(1, status.Omitted);
        Assert.Equal(2, sender.Messages.Count);
        Assert.Equal("secretariaestudios@fen.uchile.cl", sender.Messages[0].Bcc.Single());
        Assert.Equal("Fotografía Web de Docencia", sender.Messages[0].Subject);
        Assert.Contains("SECRETARIA DE ESTUDIOS", sender.Messages[0].Body);
        Assert.Equal("p1@fen.uchile.cl", sender.Messages[0].To);
        Assert.Equal("p1@test.cl", sender.Messages[0].Cc.Single());
        Assert.Equal("p2@test.cl", sender.Messages[1].To);
        Assert.Empty(sender.Messages[1].Cc);
    }

    [Fact]
    public async Task Worker_marks_partial_when_one_recipient_fails()
    {
        var sender = new PartialSender("p2@test.cl");
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe(), sender, tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        using var warmup = await client.GetAsync(PreviewUrl);
        EmailDeliverySettings.Initialize(true, true, "test@fen.uchile.cl", "secretariaestudios@fen.uchile.cl");

        using var response = await client.PostAsJsonAsync(EmailUrl, new { });
        var started = await response.Content.ReadFromJsonAsync<ApiResponse<EmailDispatchStartedDTO>>();
        EmailDispatchSnapshot? status = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(100);
            status = (await client.GetFromJsonAsync<ApiResponse<EmailDispatchSnapshot>>(
                EmailUrl + "/" + started!.Data.JobId))!.Data;
            if (status.State is "COMPLETED" or "PARTIAL" or "FAILED") break;
        }

        Assert.Equal("PARTIAL", status!.State);
        Assert.Equal(1, status.Sent);
        Assert.Equal(1, status.Failed);
        Assert.Equal(1, status.Omitted);
    }

    [Fact]
    public async Task Cancel_stops_an_active_dispatch()
    {
        var sender = new BlockingSender();
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe(), sender, tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        using var warmup = await client.GetAsync(PreviewUrl);
        EmailDeliverySettings.Initialize(true, true, "test@fen.uchile.cl", "secretariaestudios@fen.uchile.cl");

        using var response = await client.PostAsJsonAsync(EmailUrl, new { });
        var started = await response.Content.ReadFromJsonAsync<ApiResponse<EmailDispatchStartedDTO>>();
        await Task.Delay(150);
        using var cancelled = await client.PostAsJsonAsync(EmailUrl + "/" + started!.Data.JobId + "/cancel", new { });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var state = await cancelled.Content.ReadFromJsonAsync<ApiResponse<EmailDispatchSnapshot>>();
        Assert.Equal("CANCELLED", state!.Data.State);
        sender.Release();
    }

    [Fact]
    public async Task Disabled_email_does_not_create_a_job_and_reports_it()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), new Probe(), new Sender(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        using var warmup = await client.GetAsync(PreviewUrl);
        EmailDeliverySettings.Initialize(false, true, "test@fen.uchile.cl", "secretariaestudios@fen.uchile.cl");
        Assert.False(EmailDeliverySettings.IsEnabled);
        var availabilityBefore = await client.GetFromJsonAsync<ApiResponse<EmailAvailabilityDTO>>(EmailUrl + "/availability");
        Assert.False(availabilityBefore!.Data.Enabled);

        using var response = await client.PostAsJsonAsync(EmailUrl, new { });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("El envío de correo está desactivado.", problem.GetProperty("title").GetString());

        var availability = await client.GetFromJsonAsync<ApiResponse<EmailAvailabilityDTO>>(EmailUrl + "/availability");
        Assert.False(availability!.Data.Enabled);
    }

    private const string PreviewUrl =
        "/api/reports/academic-enrollment/students-without-photo/email/preview";
    private const string EmailUrl =
        "/api/reports/academic-enrollment/students-without-photo/email";

    private static WebApplicationFactory<Program> CreateFactory(
        Procedures procedures, Probe probe, IEmailSender sender, TokenFactory tokens) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                services.AddSingleton<IStudentPhotoProbe>(probe);
                services.AddSingleton<IEmailSender>(sender);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });

    internal sealed class Sender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            lock (Messages) Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    internal sealed class BlockingSender : IEmailSender
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Release() => _gate.TrySetResult();

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
            await _gate.Task.WaitAsync(cancellationToken);
    }

    internal sealed class PartialSender(string failing) : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
            message.To == failing
                ? Task.FromException(new Exception("boom"))
                : Task.CompletedTask;
    }

    internal sealed class Probe : IStudentPhotoProbe
    {
        public Task<bool> HasPhotoAsync(string personCode, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("students-without-photo-email-" + Guid.NewGuid()).Options))
    {
        public override Task<List<SpRecPersonasSinFotosResult>?>
            SpRec_SAD_SAD_PersonasSinFotosAsync(
                OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPersonasSinFotosResult>?>([
                new() { Cod_Persona = "P1", Cod_Alumno = "A/1", Nombre = "UNO", Tipo_Alumno = "IC",
                    Celular = "1", MailFen = "p1@fen.uchile.cl", E_Mail = "p1@test.cl" },
                new() { Cod_Persona = "P2", Cod_Alumno = "A/2", Nombre = "DOS", Tipo_Alumno = "IC",
                    Celular = "2", MailFen = "", E_Mail = "p2@test.cl" },
                new() { Cod_Persona = "P3", Cod_Alumno = "A/3", Nombre = "TRES", Tipo_Alumno = "IC",
                    Celular = "3", MailFen = "", E_Mail = "" },
            ]);
    }
}
