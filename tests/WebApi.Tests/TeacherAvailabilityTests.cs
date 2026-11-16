using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailability;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilityObservations;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Queries.GetTeacherAvailabilitySettings;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.UpdateTeacherAvailabilitySettings;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.DeleteTeacherAvailability;
using Application.Reports.AcademicEnrollment.TeacherAvailability.Commands.SaveTeacherAvailabilityEditor;
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

[Collection("EmailDispatch")]
public sealed class TeacherAvailabilityTests
{
    [Fact]
    public async Task Query_maps_rows_and_counts()
    {
        var response = await new GetTeacherAvailabilityHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetTeacherAvailability(null, "profesor"), default);

        Assert.Equal(("2024A", "2024 Primer Semestre", 1, 0),
            (response.Data.Period, response.Data.PeriodName, response.Data.Pending, response.Data.Ok));
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("ENECO660/01\nENECO660/02", "ELECTIVO\nELECTIVO II", "P1", "ANA PÉREZ", "2", "1",
            "Lun 10:00\nMar 11:00", "2024-03-01", "PENDIENTE", "ana@fen.uchile.cl", "0", "Sin observaciones"),
            (row.Code, row.Course, row.TeacherCode, row.Teacher, row.Necessary, row.Given,
                row.Availability, row.CreatedAt, row.State, row.Mail, row.WelcomeEmail, row.Observations));
    }

    [Fact]
    public async Task Observations_query_returns_periods_and_rows()
    {
        var response = await new GetTeacherAvailabilityObservationsHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetTeacherAvailabilityObservations(null), default);

        Assert.Equal("2024A", response.Data.Period);
        Assert.Equal("2024 Primer Semestre", Assert.Single(response.Data.Periods).Text);
        Assert.Equal("Requiere sala especial", Assert.Single(response.Data.Rows).Observations);
    }

    [Fact]
    public async Task Editor_save_updates_course_modules_and_inserts_new_modules()
    {
        var procedures = new Procedures();
        await new SaveTeacherAvailabilityEditorHandler(procedures, new ApiResponseService())
            .Handle(new SaveTeacherAvailabilityEditor(
                "2024A", "P1",
                [new("ENECO660", "01", 2)],
                [new(10, "LUN1")],
                [new(3, "MAR2")]), default);

        Assert.Equal(1, procedures.CourseTypeCalls);
        Assert.Equal(("ENECO660", "01", 2),
            (procedures.LastCourseCode, procedures.LastCourseSection, procedures.LastCourseType));
        Assert.Equal(1, procedures.ModuleUpdateCalls);
        Assert.Equal((10, "LUN1"), (procedures.LastModuleId, procedures.LastModuleCascade));
        Assert.Equal(1, procedures.ModuleInsertCalls);
        Assert.Equal(("2024A", "P1", 3, "MAR2"),
            (procedures.LastInsertPeriod, procedures.LastInsertTeacher,
                procedures.LastInsertType, procedures.LastInsertCascade));
    }

    [Fact]
    public async Task Delete_command_calls_the_legacy_sp_with_period_and_teacher()
    {
        var procedures = new Procedures();
        await new DeleteTeacherAvailabilityHandler(procedures, new ApiResponseService())
            .Handle(new DeleteTeacherAvailability("2024A", "P1"), default);

        Assert.Equal(("P1", "2024A"), (procedures.LastDeleteTeacher, procedures.LastDeletePeriod));
        Assert.Equal(1, procedures.DeleteCalls);
    }

    [Fact]
    public async Task Settings_query_maps_dates_times_and_texts()
    {
        var response = await new GetTeacherAvailabilitySettingsHandler(
            new Procedures(), new ApiResponseService())
            .Handle(new GetTeacherAvailabilitySettings(null, "profesor"), default);

        Assert.Equal(("2024A", "2024A", "2024-03-01", "2024-03-20", "08:30", "18:00",
            "15 de marzo", "20 de marzo"),
            (response.Data.Period, response.Data.ActivePeriod, response.Data.StartDate, response.Data.EndDate,
                response.Data.StartTime, response.Data.EndTime, response.Data.PendingEmailText, response.Data.LaggardEmailText));
        Assert.Equal("2024 Primer Semestre", Assert.Single(response.Data.Periods).Text);
    }

    [Fact]
    public async Task Settings_update_formats_dates_and_times_for_the_legacy_sp()
    {
        var procedures = new Procedures();
        await new UpdateTeacherAvailabilitySettingsHandler(procedures, new ApiResponseService())
            .Handle(new UpdateTeacherAvailabilitySettings(
                "2024A", "2024-03-01", "2024-03-20", "08:30", "18:00",
                "15 de marzo", "20 de marzo", true), default);

        Assert.Equal(("2024A", "01-03-2024", "20-03-2024", "08:30", "18:00", true),
            (procedures.LastPeriod, procedures.LastStartDate, procedures.LastEndDate,
                procedures.LastStartTime, procedures.LastEndTime, procedures.LastProcessPeriod));
        Assert.Equal(1, procedures.UpdateCalls);
    }

    [Fact]
    public async Task Endpoints_require_the_availability_permission()
    {
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens);
        using var signing = tokens;
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ReportUrl)).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_ACCESS_REPORT_MODULE"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(ReportUrl)).StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_AVAILABILITY"));
        using var report = await client.GetAsync(ReportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("no-store", report.Headers.CacheControl?.ToString());
        var body = await report.Content.ReadFromJsonAsync<ApiResponse<TeacherAvailabilityReportDTO>>();
        Assert.Equal("P1", Assert.Single(body!.Data.Rows).TeacherCode);

        using var forbiddenSettings = await client.GetAsync(SettingsUrl);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenSettings.StatusCode);

        client.SetBearer(tokens.CreateToken("SADInformes",
            "PERMISSION_REPORT_VIEW_AVAILABILITY", "PERMISSION_REPORT_EDIT_AVAILABILITY"));
        using var settings = await client.GetAsync(SettingsUrl);
        Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
        using var update = await client.PutAsJsonAsync(SettingsUrl, new {
            period = "2024A", startDate = "2024-03-01", endDate = "2024-03-20",
            startTime = "08:30", endTime = "18:00", pendingEmailText = "15 de marzo",
            laggardEmailText = "20 de marzo", processPeriod = false,
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        using var deleted = await client.DeleteAsync(ReportUrl + "/P1?period=2024A");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var observations = await client.GetAsync(ObservationsUrl);
        Assert.Equal(HttpStatusCode.OK, observations.StatusCode);
        var observationBody = await observations.Content
            .ReadFromJsonAsync<ApiResponse<TeacherAvailabilityObservationsReportDTO>>();
        Assert.Single(observationBody!.Data.Rows);
    }

    [Fact]
    public async Task Mass_welcome_sends_the_template_and_marks_welcome_in_the_sp()
    {
        var sender = new Sender();
        var tokens = new TokenFactory();
        using var factory = CreateFactory(new Procedures(), tokens, sender);
        using var signing = tokens;
        using var client = factory.CreateClient();
        client.SetBearer(tokens.CreateToken("SADInformes",
            "PERMISSION_REPORT_VIEW_AVAILABILITY", "PERMISSION_REPORT_SEND_AVAILABILITY_EMAIL"));
        EmailDeliverySettings.Initialize(true, true, "test@fen.uchile.cl", "secretariaestudios@fen.uchile.cl");
        TeacherAvailabilityEmailSettings.Initialize("desarrollo@fen.uchile.cl");

        using var response = await client.PostAsJsonAsync(EmailUrl,
            new { mode = "all" });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<ApiResponse<TeacherAvailabilityEmailStartedDTO>>();
        TeacherAvailabilityEmailSnapshot? status = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(50);
            status = (await client.GetFromJsonAsync<ApiResponse<TeacherAvailabilityEmailSnapshot>>(
                EmailUrl + "/" + started!.Data.JobId))!.Data;
            if (status.State is "COMPLETED" or "PARTIAL" or "FAILED" or "CANCELLED") break;
        }
        Assert.Equal("COMPLETED", status!.State);
        Assert.Equal(1, status.Sent);
        var message = Assert.Single(sender.Messages);
        Assert.Equal("Solicitud Disponibilidad Horaria", message.Subject);
        Assert.Contains("2024 Primer Semestre", message.Body);
        Assert.Equal("desarrollo@fen.uchile.cl", message.Bcc.Single());
        Assert.Equal(1, ((Procedures)factory.Services.GetRequiredService<ISadContextProcedures>()).WelcomeCalls);
    }

    private const string ReportUrl =
        "/api/reports/academic-enrollment/teacher-availability/report";
    private const string EmailUrl =
        "/api/reports/academic-enrollment/teacher-availability/email";
    private const string ObservationsUrl =
        "/api/reports/academic-enrollment/teacher-availability/observations";
    private const string SettingsUrl =
        "/api/reports/academic-enrollment/teacher-availability/settings";

    private static WebApplicationFactory<Program> CreateFactory(
        Procedures procedures, TokenFactory tokens, IEmailSender? sender = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContextProcedures>(procedures);
                if (sender is not null) services.AddSingleton(sender);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });

    internal sealed class Sender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("teacher-availability-" + Guid.NewGuid()).Options))
    {
        public int WelcomeCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public int CourseTypeCalls { get; private set; }
        public int ModuleUpdateCalls { get; private set; }
        public int ModuleInsertCalls { get; private set; }
        public string? LastCourseCode { get; private set; }
        public string? LastCourseSection { get; private set; }
        public int? LastCourseType { get; private set; }
        public int? LastModuleId { get; private set; }
        public string? LastModuleCascade { get; private set; }
        public string? LastInsertPeriod { get; private set; }
        public string? LastInsertTeacher { get; private set; }
        public int? LastInsertType { get; private set; }
        public string? LastInsertCascade { get; private set; }
        public string? LastDeleteTeacher { get; private set; }
        public string? LastDeletePeriod { get; private set; }
        public string? LastPeriod { get; private set; }
        public string? LastStartDate { get; private set; }
        public string? LastEndDate { get; private set; }
        public string? LastStartTime { get; private set; }
        public string? LastEndTime { get; private set; }
        public bool? LastProcessPeriod { get; private set; }

        public override Task<List<SpRecPeriodoDisponibilidadResult>?> SpRec_PeriodoDisponibilidadAsync(
            string? uname, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodoDisponibilidadResult>?>([
                new() { Periodo = "2024A", NombrePeriodo = "2024 Primer Semestre", Habilitado = 1, EsProfesor = 1 },
            ]);

        public override Task<List<SpRecFEProgramacionDocenteResult>?> SpRec_FE_ProgramacionDocenteAsync(
            string? periodo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecFEProgramacionDocenteResult>?>([
                new() { Codigo = "ENECO660/01<br/>ENECO660/02", Catedra = "ELECTIVO<br />ELECTIVO II", Cod_Profesor = "P1",
                    Profesor = "ANA PÉREZ", Necesarias = 2, Dadas = 1, Disp = "Lun 10:00<br/>Mar 11:00",
                    FechaCreacion = "2024-03-01", Estado = "PENDIENTE", Mail = "ana@fen.uchile.cl",
                    CorreoBienvenida = 0, Observaciones = "Sin observaciones" },
            ]);

        public override Task<List<SpRecPeriodosAcademicosResult>?> SpRec_PeriodosAcademicosAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosAcademicosResult>?>([
                new() { Periodo = "2024A", PeriodoLargo = "2024 Primer Semestre", Inscripcion = "FALSE" },
            ]);

        public override Task<List<SpRecPeriodoDisponibilidadDatosXPeriodoResult>?> SpRec_PeriodoDisponibilidad_DatosXPeriodoAsync(
            string? periodo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodoDisponibilidadDatosXPeriodoResult>?>([
                new()
                {
                    FechaInicio = new DateTime(2024, 3, 1), FechaTermino = new DateTime(2024, 3, 20),
                    HoraIncio = "08:30", HoraTermino = "18:00",
                    TextoFechaCorreoDisponibilidad = "15 de marzo", TextoFechaRezagados = "20 de marzo",
                },
            ]);

        public override Task<int> SpMOD_FE_TipoModuloCatedrasAsync(
            string? codCatedra, string? codSeccion, int? tipoModulo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            CourseTypeCalls++;
            LastCourseCode = codCatedra;
            LastCourseSection = codSeccion;
            LastCourseType = tipoModulo;
            return Task.FromResult(1);
        }

        public override Task<int> SpMOD_FE_ModulosCascadaFinalAsync(
            int? id, string? moduloCascada,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            ModuleUpdateCalls++;
            LastModuleId = id;
            LastModuleCascade = moduloCascada;
            return Task.FromResult(1);
        }

        public override Task<int> SpINS_FE_ModulosCascadaFinalAsync(
            string? periodo, string? codPersona, int? idModulo, string? moduloCascada,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            ModuleInsertCalls++;
            LastInsertPeriod = periodo;
            LastInsertTeacher = codPersona;
            LastInsertType = idModulo;
            LastInsertCascade = moduloCascada;
            return Task.FromResult(1);
        }

        public override Task<int> SpELI_DisponibilidadesAsync(
            string? codPersona, string? periodo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            DeleteCalls++;
            LastDeleteTeacher = codPersona;
            LastDeletePeriod = periodo;
            return Task.FromResult(1);
        }

        public override Task<int> SpMod_PeriodoDisponibilidad_DatosXPeriodoAsync(
            string? periodo, string? fechaInicio, string? fechaTermino, string? horaIncio,
            string? horaTermino, string? textoFechaCorreoDisponibilidad, bool? periodoProceso,
            string? textoFechaRezadadoCorreoDisponibilidad,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            UpdateCalls++;
            LastPeriod = periodo;
            LastStartDate = fechaInicio;
            LastEndDate = fechaTermino;
            LastStartTime = horaIncio;
            LastEndTime = horaTermino;
            LastProcessPeriod = periodoProceso;
            return Task.FromResult(1);
        }

        public override Task<int> SpMOD_FE_BienvenidaAsync(
            string? periodo, string? codProfesor, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            WelcomeCalls++;
            return Task.FromResult(1);
        }

        public override Task<List<SpRecFEDisponibilidadObservacionesResult>?> SpRec_FE_DisponibilidadObservacionesAsync(
            string? periodo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecFEDisponibilidadObservacionesResult>?>([
                new() { Cod_Profesor = "P1", Profesor = "ANA PÉREZ", Observaciones = "Requiere sala especial" },
            ]);
    }
}
