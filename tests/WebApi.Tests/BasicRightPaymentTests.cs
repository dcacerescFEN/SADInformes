using System.Data;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.BasicRightPayment.Commands.UpdateBasicRightPayments;
using Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPaymentFilters;
using Application.Reports.Postgraduate.BasicRightPayment.Queries.GetBasicRightPayments;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

public sealed class BasicRightPaymentTests
{
    [Fact]
    public async Task Filters_and_report_include_amount_rows_and_photo()
    {
        using var data = Data();
        var procedures = new Procedures(data);
        var filters = await new GetBasicRightPaymentFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetBasicRightPaymentFilters(null), default);
        Assert.Equal(new[] { "MBA" }, filters.Data.StudentTypes.Select(row => row.Value));
        Assert.Equal(new[] { "PT" }, filters.Data.Modalities.Select(row => row.Value));
        Assert.Equal(new[] { "2024", "2023" }, filters.Data.Years.Select(row => row.Value));

        var report = await new GetBasicRightPaymentsHandler(procedures, data, new Settings(), new ApiResponseService())
            .Handle(new GetBasicRightPayments("MBA", "PT", "2024", "2024", "Todos", true), default);
        Assert.Equal(100000, report.Data.Amount);
        var row = Assert.Single(report.Data.Rows);
        Assert.Equal(("ANA PÉREZ", "11111111-1", "A1", "2024A", "REGULAR", 6.5m, false,
            "https://photos.test/P1.jpg"),
            (row.Name, row.Rut, row.StudentCode, row.Entry, row.Situation, row.Average, row.Paid, row.PhotoUrl));
    }

    [Fact]
    public async Task Save_uses_the_server_user_and_commits_one_transaction()
    {
        using var data = Data();
        var procedures = new Procedures(data);
        var response = await new UpdateBasicRightPaymentsHandler(data, procedures, new ApiResponseService())
            .Handle(new UpdateBasicRightPayments(
                new("2024", [new("A1", true)]), "operator"), default);
        Assert.Equal((2024, 1), (int.Parse(response.Data.PaymentYear), response.Data.Saved));
        Assert.Equal(("A1", "2024", true, "operator"), Assert.Single(procedures.Writes));
        Assert.Equal(1, data.Commits);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new UpdateBasicRightPaymentsHandler(data, procedures, new ApiResponseService())
                .Handle(new UpdateBasicRightPayments(new("2023", [new("A1", true)]), "operator"), default));
    }

    [Fact]
    public async Task Endpoints_require_view_and_manage_permissions_and_export_seven_columns()
    {
        using var data = Data();
        var procedures = new Procedures(data);
        var tokens = new TokenFactory();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:AppConfig"] = "" }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISadContext>(data);
                services.AddSingleton<ISadContextProcedures>(procedures);
                tokens.Configure(services); tokens.UseClient(services, "SADInformes");
            });
        });
        using var signing = tokens; using var client = factory.CreateClient();
        const string reportUrl =
            "/api/reports/postgraduate/basic-right-payment/report?studentType=MBA&modality=PT&entryYear=2024&paymentYear=2024&paymentStatus=Todos";
        Assert.Equal((int)HttpStatusCode.Unauthorized, (int)(await client.GetAsync(reportUrl)).StatusCode);
        client.SetBearer(tokens.CreateToken("SADInformes", "PERMISSION_REPORT_VIEW_POSTGRADUATE"));
        using var report = await client.GetAsync(reportUrl);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        using var download = await client.GetAsync(
            "/api/reports/postgraduate/basic-right-payment/download?studentType=MBA&modality=PT&entryYear=2024&paymentYear=2024&paymentStatus=Todos");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        using var bytes = new MemoryStream(await download.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var xml = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Assert.Equal(7, XDocument.Load(xml).Descendants(ns + "row").First().Descendants(ns + "t").Count());

        using var saved = await client.PutAsJsonAsync(
            "/api/reports/postgraduate/basic-right-payment/payments",
            new { paymentYear = "2024", rows = new[] { new { studentCode = "A1", paid = true } } });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
    }

    internal sealed class Settings : IStudentPhotoSettings
    {
        public string BaseUrl => "https://photos.test/";
    }

    private static FakeData Data()
    {
        var data = new FakeData();
        data.PagosDerechoBasicoAnualMontos.Add(new PagoDerechoBasicoAnualMonto { Año = "2024", Monto = 100000 });
        data.Alumnos.Add(new Alumno
        {
            Matricula = "1", SemIngresoDecreto = "2024F", CodAlumno = "A1", CodPersona = "P1",
            TipoAlumno = "MBA", Modalidad = "PT",
        });
        data.SaveChanges();
        return data;
    }

    internal sealed class FakeData() : SadContext(new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase("basic-right-" + Guid.NewGuid()).Options)
    {
        public int Commits { get; private set; }
        public override Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel level, CancellationToken ct = default)
        {
            Assert.Equal(IsolationLevel.Serializable, level);
            return Task.FromResult<IDbContextTransaction>(new Transaction(this));
        }
        private sealed class Transaction(FakeData data) : IDbContextTransaction
        {
            public Guid TransactionId { get; } = Guid.NewGuid();
            public void Commit() => data.Commits++;
            public Task CommitAsync(CancellationToken ct = default) { Commit(); return Task.CompletedTask; }
            public void Rollback() { }
            public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    internal sealed class Procedures(FakeData data) : SadContextProcedures(data)
    {
        public List<(string? Code, string? Year, bool? Paid, string? User)> Writes { get; } = [];
        public override Task<List<SpRecTiposAlumnoXEscuelaResult>?> SpRec_TiposAlumnoXEscuelaAsync(
            string? escuela, string? incluirTodos, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecTiposAlumnoXEscuelaResult>?>([
                new() { Tipo = "Todos", Nombre = "Todos" }, new() { Tipo = "MBA", Nombre = "MBA" },
            ]);
        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(
            string? tipo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>([new() { Tipo = "MBA", Modalidad = "PT" }]);
        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecEnrollmentYearsResult>?>([new() { Año = "2024" }, new() { Año = "2023" }]);
        public override Task<List<SpRecAlumnosXPagoDerechoBasicoResult>?> SpRec_AlumnosXPago_Derecho_BasicoAsync(
            string? tipo_Alumno, string? modalidad, string? añoIngreso, string? año, string? pago,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("MBA", "PT", "2024", "2024", "Todos"),
                (tipo_Alumno, modalidad, añoIngreso, año, pago));
            return Task.FromResult<List<SpRecAlumnosXPagoDerechoBasicoResult>?>([
                new() { Posicion = 1, Nombre = "ANA PÉREZ", Rut = "11111111-1", Código = "A1",
                    Ingreso = "2024A", Situación = "REGULAR", Promedio = 6.5m, Pago = false },
            ]);
        }
        public override Task<int> SpCre_AlumnosXPago_Derecho_BasicoAsync(
            string? cod_Alumno, string? año, bool? pago, string? usuario,
            CancellationToken? cancellationToken = default)
        {
            Writes.Add((cod_Alumno, año, pago, usuario));
            return Task.FromResult(1);
        }
    }
}
