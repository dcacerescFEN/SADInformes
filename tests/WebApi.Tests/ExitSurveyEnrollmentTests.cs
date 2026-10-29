using System.Data;
using Application.Common.Interfaces;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Commands.CreateExitSurveyEnrollment;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Common;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollment;
using Application.Reports.Postgraduate.ExitSurveyEnrollment.Queries.GetExitSurveyEnrollmentFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace WebApi.Tests;

public sealed class ExitSurveyEnrollmentTests
{
    private static readonly DateTime ExitDate = new(2023, 12, 15, 13, 45, 0);
    private static readonly DateTime DegreeDate = new(2024, 1, 10);

    [Fact]
    public async Task Filters_keep_Todos_use_configured_user_and_depend_on_type()
    {
        var data = new FakeData();
        var handler = new GetExitSurveyEnrollmentFiltersHandler(new Procedures(data), new ApiResponseService(), new Settings());
        var result = await handler.Handle(new GetExitSurveyEnrollmentFilters(null), default);
        Assert.Equal(new[] { "Todos", "MBA", "MGP" }, result.Data.StudentTypes.Select(row => row.Value));
        Assert.Equal("Todos", data.ModalityType);
        Assert.Equal("configured-user", data.CatalogueUser);
        Assert.Equal(new[] { "2024", "2023" }, result.Data.Years);
        await handler.Handle(new GetExitSurveyEnrollmentFilters("MGP"), default);
        Assert.Equal("MGP", data.ModalityType);
    }

    [Fact]
    public async Task Listing_deduplicates_codes_formats_dates_and_preserves_progress()
    {
        var data = new FakeData();
        var response = await new GetExitSurveyEnrollmentHandler(new Procedures(data), new ApiResponseService())
            .Handle(new GetExitSurveyEnrollment("Todos", "Todos", "2024"), default);
        Assert.Equal(2, response.Data.Total);
        var first = response.Data.Rows[0];
        Assert.Equal(("ANA PÉREZ", "P1", "100-20241", "2024A", "15-12-2023", "10-01-2024", true, "40%"),
            (first.Name, first.PersonCode, first.StudentCode, first.EntrySemester, first.ExitDate, first.DegreeDate, first.Enrolled, first.Progress));
        var second = response.Data.Rows[1];
        Assert.False(second.Enrolled); Assert.Null(second.Progress);
    }

    [Fact]
    public async Task Batch_derives_period_and_date_server_side_skips_existing_and_commits_once()
    {
        var data = new FakeData();
        data.EncuestaSalidaGraduados.Add(new() { Folio = 1, CodAlumno = "200-20241" });
        await data.SaveChangesAsync();
        var result = await new CreateExitSurveyEnrollmentHandler(data, new Procedures(data), new ApiResponseService(), new Settings())
            .Handle(new CreateExitSurveyEnrollment(new("Todos", "Todos", "2024", ["100-20241", "200-20241"], true)), default);
        Assert.Equal(new[] { "100-20241" }, data.InsertedCodes);
        Assert.Equal(new[] { "20241" }, data.Periods);
        Assert.Equal(new[] { "15/12/2023" }, data.Dates.Distinct());
        Assert.Equal((1, 1), (result.Data.Enrolled, result.Data.Skipped));
        Assert.Equal(1, data.Commits); Assert.Equal(0, data.Rollbacks);
        Assert.False(data.ClientControlledDateAccepted);
    }

    [Fact]
    public async Task Null_exit_date_is_sent_as_null_instead_of_empty_or_1900()
    {
        var data = new FakeData { NullExitDate = true };
        var result = await new CreateExitSurveyEnrollmentHandler(data, new Procedures(data), new ApiResponseService(), new Settings())
            .Handle(new CreateExitSurveyEnrollment(new("Todos", "Todos", "2024", ["100-20241"], true)), default);
        Assert.Null(Assert.Single(data.Dates));
        Assert.Equal(1, result.Data.Enrolled);
    }

    [Fact]
    public async Task Batch_all_or_nothing_rolls_back_when_a_procedure_fails()
    {
        var data = new FakeData { FailCode = "200-20241" };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CreateExitSurveyEnrollmentHandler(data, new Procedures(data), new ApiResponseService(), new Settings())
                .Handle(new CreateExitSurveyEnrollment(new("Todos", "Todos", "2024", ["100-20241", "200-20241"], true)), default));
        Assert.Equal(0, data.EncuestaSalidaGraduados.Count());
        Assert.Equal(0, data.Commits); Assert.Equal(1, data.Rollbacks);
    }

    [Fact]
    public async Task Batch_rolls_back_when_procedure_silently_omits_postcondition()
    {
        var data = new FakeData { SilentCode = "200-20241" };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CreateExitSurveyEnrollmentHandler(data, new Procedures(data), new ApiResponseService(), new Settings())
                .Handle(new CreateExitSurveyEnrollment(new("Todos", "Todos", "2024", ["100-20241", "200-20241"], true)), default));
        Assert.Equal(0, data.EncuestaSalidaGraduados.Count());
        Assert.Equal(0, data.Commits); Assert.Equal(1, data.Rollbacks);
    }

    [Theory]
    [InlineData(false, "Todos", "Todos", "2024", "100-20241", "Debe confirmar la inscripción.")]
    [InlineData(true, "Todos", "Todos", "2024", "999-20241", "La selección cambió o incluye alumnos ya inscritos.")]
    [InlineData(true, "Todos", "Todos", "2024", "100-20241,100-20241", "Seleccione alumnos válidos sin duplicados.")]
    [InlineData(true, "Todos", "Todos", "2024", "", "Seleccione alumnos válidos sin duplicados.")]
    [InlineData(true, "Todos", "Todos", "2024A", "100-20241", "El año no es válido.")]
    [InlineData(true, "X", "Todos", "2024", "100-20241", "El tipo de alumno no es válido.")]
    public async Task Invalid_or_unauthorized_batch_never_writes(bool confirmed, string type, string modality, string year,
        string codes, string expected)
    {
        var data = new FakeData();
        var error = await Assert.ThrowsAnyAsync<Exception>(() =>
            new CreateExitSurveyEnrollmentHandler(data, new Procedures(data), new ApiResponseService(), new Settings())
                .Handle(new CreateExitSurveyEnrollment(new(type, modality, year, codes.Split(',', StringSplitOptions.RemoveEmptyEntries), confirmed)), default));
        Assert.Contains(expected, error.Message);
        Assert.Empty(data.InsertedCodes); Assert.Equal(0, data.Writes);
    }

    [Fact]
    public async Task Already_inscribed_selection_is_idempotent_without_procedure_calls()
    {
        var data = new FakeData();
        data.EncuestaSalidaGraduados.AddRange(new() { Folio = 1, CodAlumno = "100-20241" }, new() { Folio = 2, CodAlumno = "200-20241" });
        await data.SaveChangesAsync();
        var result = await new CreateExitSurveyEnrollmentHandler(data, new Procedures(data), new ApiResponseService(), new Settings())
            .Handle(new CreateExitSurveyEnrollment(new("Todos", "Todos", "2024", ["100-20241", "200-20241"], true)), default);
        Assert.Empty(data.InsertedCodes);
        Assert.Equal((0, 2), (result.Data.Enrolled, result.Data.Skipped));
        Assert.Equal(1, data.Commits);
    }

    internal sealed class Settings : IPostgraduateSettings { public string User => "configured-user"; }

    internal sealed class FakeData() : SadContext(new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        public string? FailCode { get; init; }
        public string? SilentCode { get; init; }
        public bool FailNext { get; set; }
        public bool NullExitDate { get; init; }
        public bool FailReads { get; init; }
        public string? ModalityType { get; private set; }
        public string? CatalogueUser { get; private set; }
        public List<string> InsertedCodes { get; } = [];
        public List<string> Periods { get; } = [];
        public List<string?> Dates { get; } = [];
        public int Writes { get; private set; }
        public int Commits { get; private set; }
        public int Rollbacks { get; private set; }
        public bool ClientControlledDateAccepted => Dates.Any(date => date != "15/12/2023");

        public override Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel level, CancellationToken ct = default)
        {
            Assert.Equal(IsolationLevel.Serializable, level);
            return Task.FromResult<IDbContextTransaction>(new Transaction(this));
        }
        public void Record(string code, string period, string? date)
        {
            if (code == FailCode || FailNext) throw new InvalidOperationException("private-sql procedure failure");
            Writes++;
            if (code == SilentCode) return;
            InsertedCodes.Add(code); Periods.Add(period); Dates.Add(date);
            EncuestaSalidaGraduados.Add(new() { Folio = 1000 + Writes, CodAlumno = code });
            SaveChanges();
        }
        public void MarkModalityType(string? value) => ModalityType = value;
        public void MarkCatalogueUser(string? value) => CatalogueUser = value;
        private sealed class Transaction(FakeData data) : IDbContextTransaction
        {
            private readonly HashSet<int> folios = data.EncuestaSalidaGraduados.Select(row => row.Folio).ToHashSet();
            private bool committed;
            public Guid TransactionId { get; } = Guid.NewGuid();
            public void Commit() { committed = true; data.Commits++; }
            public Task CommitAsync(CancellationToken ct = default) { Commit(); return Task.CompletedTask; }
            public void Rollback()
            {
                data.EncuestaSalidaGraduados.RemoveRange(data.EncuestaSalidaGraduados.Where(row => !folios.Contains(row.Folio)));
                data.SaveChanges(); data.Writes = 0; data.InsertedCodes.Clear(); data.Periods.Clear(); data.Dates.Clear(); data.Rollbacks++;
            }
            public Task RollbackAsync(CancellationToken ct = default) { Rollback(); return Task.CompletedTask; }
            public void Dispose() { if (!committed) Rollback(); }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }

    internal sealed class Procedures(FakeData data) : SadContextProcedures(data)
    {
        public override Task<List<SpRecEncuestaGraduadosTiposAlumnoResult>?> spRec_EncuestaGraduados_TiposAlumnoAsync(
            string? incluirTodos, string? usuario, OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            Assert.Equal("SI", incluirTodos);
            if (data.FailReads) throw new InvalidOperationException("private-read");
            data.MarkCatalogueUser(usuario);
            return Task.FromResult<List<SpRecEncuestaGraduadosTiposAlumnoResult>?>(
            [new() { Tipo = "Todos", Nombre = "Todos" }, new() { Tipo = "MBA", Nombre = "MBA" }, new() { Tipo = "MGP", Nombre = "MGP" }]);
        }
        public override Task<List<SpRecEncuestaGraduadosModalidadesResult>?> spRec_EncuestaGraduados_ModalidadesAsync(
            string? tipo, OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            data.MarkModalityType(tipo);
            if (data.FailReads) throw new InvalidOperationException("private-read");
            return Task.FromResult<List<SpRecEncuestaGraduadosModalidadesResult>?>(
            [new() { Tipo = "Todos", Modalidad = "Todos" }, new() { Tipo = tipo, Modalidad = "PT" }]);
        }
        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(string? incluirTodos, string? tipoAlumno,
            OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            Assert.Equal(("NO", "Todos"), (incluirTodos, tipoAlumno));
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([new() { Año = "2024" }, new() { Año = "2023" }]);
        }
        public override Task<List<SpRecEncuestaGraduadosAlumnosResult>?> spRec_EncuestaGraduados_AlumnosAsync(
            string? tipo_Alumno, string? modalidad, string? año, OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            Assert.Equal(("Todos", "Todos", "2024"), (tipo_Alumno, modalidad, año));
            if (data.FailReads) throw new InvalidOperationException("private-read");
            return Task.FromResult<List<SpRecEncuestaGraduadosAlumnosResult>?>(
            [
                new() { Nombre = "ANA PÉREZ", CodPersona = "P1", CodAlumno = "100-20241", SemIngreso = "2024A", TipoAlumno = "MBA", Modalidad = "PT", FechaEgreso = data.NullExitDate ? null : ExitDate, FechaTitulacion = DegreeDate, Inscrito = "SI", Porcentaje = "40%" },
                new() { Nombre = "ANA PÉREZ", CodPersona = "P1", CodAlumno = "100-20241", SemIngreso = "2024A", TipoAlumno = "MBA", Modalidad = "PT", FechaEgreso = data.NullExitDate ? null : ExitDate, FechaTitulacion = DegreeDate, Inscrito = "SI", Porcentaje = "40%" },
                new() { Nombre = "LUIS SOTO", CodPersona = "P2", CodAlumno = "200-20241", SemIngreso = "2024A", TipoAlumno = "MBA", Modalidad = "PT", FechaEgreso = data.NullExitDate ? null : ExitDate, FechaTitulacion = DegreeDate, Inscrito = "NO" },
            ]);
        }
        public override Task<int> spCre_EncuestaGraduados_Ins_AlumnoAsync(string? cod_Alumno, string? periodo,
            string? fecha_Egreso, OutputParameter<int>? returnValue = null, CancellationToken? ct = default)
        {
            data.Record(cod_Alumno!, periodo!, fecha_Egreso);
            return Task.FromResult(0);
        }
    }
}
