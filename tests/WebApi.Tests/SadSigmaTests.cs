using Application.Common.Interfaces;
using Application.Reports.Postgraduate.SadSigma.Queries.GetSadSigma;
using Application.Reports.Postgraduate.SadSigma.Queries.GetSadSigmaFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.SigmaRemuneraciones;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebApi.Tests;

public sealed class SadSigmaTests
{
    [Fact]
    public async Task Filters_expose_only_valid_four_digit_years()
    {
        var result = await new GetSadSigmaFiltersHandler(new Procedures(), new ApiResponseService())
            .Handle(new GetSadSigmaFilters(), default);
        Assert.Equal(new[] { "2026", "2024" }, result.Data.Years);
    }

    [Fact]
    public async Task Report_enriches_sigma_and_preserves_all_thirty_eight_columns()
    {
        var procedures = new Procedures();
        await using var sigma = CreateSigma();
        var response = await new GetSadSigmaHandler(procedures, sigma, new ApiResponseService())
            .Handle(new GetSadSigma("2024"), default);
        Assert.Equal(new[] { "2024" }, procedures.Arguments);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("2", "IMP-1", "Gestión", "ADM", "2024F", "MBA600", "01", "Ana Pérez",
            "P/1", "AQ", "4.5", "01/03/2024", "30/06/2024", "2024", "FULL TIME", "Gestión Estratégica",
            "Cátedra", "MBA", "SUNRISE", "Confirmado", "Diurno", "20", "15", "5", "6,25", "SI",
            "100", "90", "200", "180", "300", "270", "SI", "01/03/2024 10:30:00",
            "01/02/2024 09:00:00", "usuario", "Observación", "usuario 01-03-2024 10:00"),
            (row.Id, row.Cod, row.Description, row.Area, row.Period, row.CourseCode, row.Section,
                row.TeacherName, row.PersonCode, row.Aqpq, row.DedicatedHours, row.Start, row.End,
                row.Promotion, row.TeacherType, row.CourseName, row.CourseType, row.Program,
                row.Modality, row.Confirmed, row.ScheduleType, row.TotalStudents,
                row.PostgraduateStudents, row.UndergraduateStudents, row.TeachingEvaluationAverage,
                row.ExistsInVista, row.Scenario1Projected, row.Scenario1Actual,
                row.Scenario2Projected, row.Scenario2Actual, row.Scenario3Projected,
                row.Scenario3Actual, row.ExistsInSigma, row.CreatedInSigma, row.ConfirmationDate,
                row.ConfirmationUser, row.CourseObservations, row.ObservationUser));
    }

    [Fact]
    public async Task Missing_sigma_configuration_or_lookup_uses_legacy_NO_fallback()
    {
        await using var sigma = CreateSigma(configured: false, seed: false);
        var response = await new GetSadSigmaHandler(
                new Procedures(), sigma, new ApiResponseService())
            .Handle(new GetSadSigma("2024"), default);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal("NO", row.ExistsInSigma);
        Assert.Equal("", row.CreatedInSigma);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Todos")]
    [InlineData("%")]
    [InlineData("202")]
    [InlineData("2024A")]
    [InlineData("2025")]
    public async Task Invalid_or_out_of_catalog_year_never_calls_sql_or_sigma(string? year)
    {
        var procedures = new Procedures();
        await using var sigma = CreateSigma();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetSadSigmaHandler(procedures, sigma, new ApiResponseService())
                .Handle(new GetSadSigma(year), default));
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_result_is_valid()
    {
        await using var sigma = CreateSigma(seed: false);
        var response = await new GetSadSigmaHandler(
                new Procedures { Empty = true }, sigma, new ApiResponseService())
            .Handle(new GetSadSigma("2024"), default);
        Assert.Empty(response.Data.Rows);
        Assert.Equal(0, response.Data.Total);
    }

    internal static SigmaRemuneracionesContext CreateSigma(bool configured = true, bool seed = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configured
                ? new Dictionary<string, string?> { ["ConnectionStrings:SigmaRemuneraciones"] = "in-memory" }
                : new Dictionary<string, string?>())
            .Build();
        var options = new DbContextOptionsBuilder<SigmaRemuneracionesContext>()
            .UseInMemoryDatabase("sad-sigma-" + Guid.NewGuid())
            .Options;
        var context = new SigmaRemuneracionesContext(options, configuration);
        if (!seed) return context;
        context.Remuneraciones.Add(new Remuneracion { Id = 1, CreatedAt = new(2024, 3, 1, 10, 30, 0) });
        context.Prestaciones.Add(new Prestacion { Id = 1, RemuneracionId = 1, CodImp = "IMP-1" });
        context.SaveChanges();
        return context;
    }

    internal sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public string?[]? Arguments { get; private set; }
        public bool Empty { get; init; }
        public bool Fail { get; init; }

        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("NO", "Todos"), (incluirTodos, tipoAlumno));
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([
                new() { Año = "Todos" }, new() { Año = "2026" }, new() { Año = "2024" },
            ]);
        }

        public override Task<List<SpRecCatedrasAreasXPagosProfesoresResult>?> SpRec_CatedrasAreasXPagosProfesoresAsync(
            string? año, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Arguments = [año];
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecCatedrasAreasXPagosProfesoresResult>?>(Empty ? null :
            [
                new()
                {
                    ID = 2, COD = "IMP-1", descripcion = "Gestión", area = "ADM",
                    periodo = "2024F", cod_catedra = "MBA600", cod_seccion = "01",
                    Nombre_Profesor = "Ana Pérez", Cod_Persona = "P/1", AQPQ = "AQ",
                    HorasDedicadas = 4.5, Inicio = "01/03/2024", Termino = "30/06/2024",
                    Promocion = "2024", Tipo_Profesor = "FULL TIME",
                    nom_catedra = "Gestión Estratégica", TipoCatedra = "Cátedra",
                    Programa = "MBA", Modalidad = "SUNRISE", Confirmado = "Confirmado",
                    Tipo_Horario = "Diurno", Alumnos_Totales = 20, Alumnos_Postgrado = 15,
                    Alumnos_Pregrado = 5, Promedio_Ev_Docente = "6,25", Existe_En_Vista = "SI",
                    Escenario1Proyectado = 100, Escenario1Real = 90,
                    Escenario2Proyectado = 200, Escenario2Real = 180,
                    Escenario3Proyectado = 300, Escenario3Real = 270, Existe_En_Sigma = "",
                    Fecha_Confirmacion = new DateTime(2024, 2, 1, 9, 0, 0),
                    Usuario_Confirmacion = "usuario", Observaciones_Curso = "Observación",
                    Usuario_Observ = "usuario 01-03-2024 10:00",
                },
            ]);
        }
    }
}
