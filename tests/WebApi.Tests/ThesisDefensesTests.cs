using Application.Common.Interfaces;
using Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefenses;
using Application.Reports.Postgraduate.ThesisDefenses.Queries.GetThesisDefensesFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class ThesisDefensesTests
{
    [Fact]
    public async Task Filters_return_Todos_and_reload_modalities_for_a_concrete_type()
    {
        var procedures = new Procedures();
        var all = await new GetThesisDefensesFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetThesisDefensesFilters("Todos"), default);
        Assert.Equal(new[] { "Todos", "MBA", "MGP" }, all.Data.StudentTypes);
        Assert.Equal(new[] { "Todos" }, all.Data.Modalities);
        var mba = await new GetThesisDefensesFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetThesisDefensesFilters("MBA"), default);
        Assert.Equal(new[] { "Todos", "PT", "FT" }, mba.Data.Modalities);
        Assert.Equal("MBA", procedures.ModalityType);
    }

    [Fact]
    public async Task Report_calls_both_procedures_with_the_same_four_values_and_maps_both_shapes()
    {
        var procedures = new Procedures();
        var response = await new GetThesisDefensesHandler(procedures, new ApiResponseService())
            .Handle(new GetThesisDefenses("2024", "MBA", "PT", "2020"), default);
        Assert.Equal(new[] { "2024", "MBA", "PT", "2020" }, procedures.GciArguments);
        Assert.Equal(procedures.GciArguments, procedures.Gci2Arguments);
        var coordination = Assert.Single(response.Data.Coordination);
        Assert.Equal(("A/1", "Ana Pérez", "MBA", "PT", "2020", "2024F", "5.5", "Guía"),
            (coordination.StudentCode, coordination.StudentName, coordination.Program,
                coordination.Modality, coordination.EntryYear, coordination.Period,
                coordination.Grade, coordination.Guide));
        var treasury = Assert.Single(response.Data.Treasury);
        Assert.Equal(("2024F", "MBA600", "01", "P/1", "Ana Pérez", "GUIA", "A/1", "5.5",
            "Ana Pérez", "MBA", "PT", "2020", "15/06/2024 00:00:00", "Vigente"),
            (treasury.Period, treasury.CourseCode, treasury.Section, treasury.ProfessorCode,
                treasury.ProfessorName, treasury.Type, treasury.StudentCode, treasury.Grade,
                treasury.StudentName, treasury.Program, treasury.Modality, treasury.EntryYear,
                treasury.ExamDate, treasury.ExamStatus));
        Assert.Equal(1, response.Data.CoordinationTotal);
        Assert.Equal(1, response.Data.TreasuryTotal);
    }

    [Fact]
    public async Task Todos_is_sent_unchanged_and_empty_results_are_valid()
    {
        var procedures = new Procedures { Empty = true };
        var response = await new GetThesisDefensesHandler(procedures, new ApiResponseService())
            .Handle(new GetThesisDefenses("Todos", "Todos", "Todos", "Todos"), default);
        Assert.Equal(new[] { "Todos", "Todos", "Todos", "Todos" }, procedures.GciArguments);
        Assert.Empty(response.Data.Coordination);
        Assert.Empty(response.Data.Treasury);
    }

    [Theory]
    [InlineData("202", "MBA", "PT", "2020")]
    [InlineData("2024", "OTHER", "PT", "2020")]
    [InlineData("2024", "MBA", "OTHER", "2020")]
    [InlineData("2024", "MBA", "PT", "202")]
    [InlineData("2025", "MBA", "PT", "2020")]
    public async Task Invalid_selection_never_calls_a_report_procedure(
        string defense, string type, string modality, string entry)
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetThesisDefensesHandler(procedures, new ApiResponseService())
                .Handle(new GetThesisDefenses(defense, type, modality, entry), default));
        Assert.Equal(0, procedures.ReportCalls);
    }

    internal sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public string?[]? GciArguments { get; private set; }
        public string?[]? Gci2Arguments { get; private set; }
        public string? ModalityType { get; private set; }
        public int ReportCalls { get; private set; }
        public bool Empty { get; init; }
        public bool Fail { get; init; }

        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("NO", "Todos"), (incluirTodos, tipoAlumno));
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([
                new() { Año = "Todos" }, new() { Año = "2026" }, new() { Año = "2024" },
                new() { Año = "2020" },
            ]);
        }

        public override Task<List<SpRecTipoAlumnosPostgradoResult>?> SpRecTipoAlumnosPostgradoAsync(
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecTipoAlumnosPostgradoResult>?>([
                new() { Tipo_Alumno = "MBA" }, new() { Tipo_Alumno = "MGP" },
            ]);

        public override Task<List<SpRecModalidadesPostgradoResult>?> SpRecModalidadesPostgradoAsync(
            string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            ModalityType = tipoAlumno;
            return Task.FromResult<List<SpRecModalidadesPostgradoResult>?>([
                new() { Modalidad = "PT" }, new() { Modalidad = "FT" },
            ]);
        }

        public override Task<List<SpRecInformePostgradoProfesoresGCIResult>?> SpRec_InformePostgradoProfesoresGCIAsync(
            string? anioDefensa, string? tipoAlumno, string? modalidad, string? anioIngreso,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            ReportCalls++;
            GciArguments = [anioDefensa, tipoAlumno, modalidad, anioIngreso];
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecInformePostgradoProfesoresGCIResult>?>(Empty ? null :
            [
                new()
                {
                    Cod_Alumno = "A/1", Nombre = "Ana Pérez", Programa = "MBA", Modalidad = "PT",
                    AñoIngreso = "2020", FechaExamen = "15/06/2024", Periodo = "2024F",
                    Cod_Catedra = "MBA600", Cod_Seccion = "01", Nota = "5.5",
                    Estado_Examen = "Aprobado", TemaNombreTesis = "Tesis", GUIA = "Guía",
                    COGUIA = "CoGuía", INFORMANTE1 = "Inf1", INFORMANTE2 = "Inf2",
                    INFORMANTE3 = "Inf3", CentroCosto = "Centro", AñoDefensa = "2024",
                    FechaResolucion = "01/07/2024",
                },
            ]);
        }

        public override Task<List<SpRecInformePostgradoProfesoresGCI2Result>?> SpRec_InformePostgradoProfesoresGCI2Async(
            string? anioDefensa, string? tipoAlumno, string? modalidad, string? anioIngreso,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            ReportCalls++;
            Gci2Arguments = [anioDefensa, tipoAlumno, modalidad, anioIngreso];
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecInformePostgradoProfesoresGCI2Result>?>(Empty ? null :
            [
                new()
                {
                    Periodo = "2024F", Cod_Catedra = "MBA600", Cod_Seccion = "01",
                    Cod_Profesor = "P/1", NombreProfesor = "Ana Pérez", Tipo = "GUIA",
                    Cod_Alumno = "A/1", Nota = "5.5", Estudiante = "Ana Pérez", Programa = "MBA",
                    Modalidad = "PT", AñoIngreso = "2020", TemaNombreTesis = "Tesis",
                    Fecha_Examen = new DateTime(2024, 6, 15), Estado_Examen = "Vigente",
                    CentroCosto = "Centro", FechaResolucion = "01/07/2024",
                },
            ]);
        }
    }
}
