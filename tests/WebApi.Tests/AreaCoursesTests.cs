using Application.Common.Interfaces;
using Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCourses;
using Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCoursesFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class AreaCoursesTests
{
    [Fact]
    public async Task Filters_expose_only_valid_four_digit_years()
    {
        var result = await new GetAreaCoursesFiltersHandler(
                new Procedures(), new ApiResponseService())
            .Handle(new GetAreaCoursesFilters(), default);
        Assert.Equal(new[] { "2026", "2024" }, result.Data.Years);
    }

    [Fact]
    public async Task Report_preserves_order_and_all_twenty_eight_columns()
    {
        var procedures = new Procedures();
        var response = await new GetAreaCoursesHandler(procedures, new ApiResponseService())
            .Handle(new GetAreaCourses("2024", "2026"), default);
        Assert.Equal(new[] { "2024", "2026" }, procedures.Arguments);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("2", "Gestión", "ADM", "2024F", "MBA600", "01", "4", "Ana Pérez",
            "P/1", "FULL TIME", "01/03/2024", "30/06/2024", "MBA", "SUNRISE", "2024",
            "Gestión Estratégica", "Cátedra", "Diurno", "AQ", "SA", "Confirmado", "4.5",
            "20", "15", "5", "6,25", "Observación", "usuario 01-03-2024 10:00"),
            (row.Id, row.Description, row.Area, row.Period, row.CourseCode, row.Section,
                row.Credits, row.TeacherName, row.PersonCode, row.TeacherType, row.Start, row.End,
                row.Program, row.Modality, row.Promotion, row.CourseName, row.CourseType,
                row.ScheduleType, row.Aqpq, row.TeachingCategory, row.Confirmed, row.DedicatedHours,
                row.TotalStudents, row.PostgraduateStudents, row.UndergraduateStudents,
                row.TeachingEvaluationAverage, row.CourseObservations, row.ObservationUser));
        Assert.Equal(1, response.Data.Total);
    }

    [Theory]
    [InlineData(null, "2024")]
    [InlineData("2024", null)]
    [InlineData("Todos", "2024")]
    [InlineData("2024", "%")]
    [InlineData("202", "2024")]
    [InlineData("2024", "2024A")]
    [InlineData("2025", "2024")]
    [InlineData("2024", "2025")]
    public async Task Invalid_or_out_of_catalog_year_never_calls_the_procedure(string? year1, string? year2)
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetAreaCoursesHandler(procedures, new ApiResponseService())
                .Handle(new GetAreaCourses(year1, year2), default));
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_result_is_valid()
    {
        var response = await new GetAreaCoursesHandler(
                new Procedures { Empty = true }, new ApiResponseService())
            .Handle(new GetAreaCourses("2024", "2024"), default);
        Assert.Empty(response.Data.Rows);
        Assert.Equal(0, response.Data.Total);
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

        public override Task<List<SpRecCatedrasAreasReportePostgradoResult>?> SpRec_CatedrasAreas_ReportePostgradoAsync(
            string? año1, string? año2, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Arguments = [año1, año2];
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecCatedrasAreasReportePostgradoResult>?>(Empty ? null :
            [
                new()
                {
                    ID = 2, descripcion = "Gestión", area = "ADM", periodo = "2024F",
                    cod_catedra = "MBA600", cod_seccion = "01", Creditos = 4,
                    Nombre_Profesor = "Ana Pérez", Cod_Persona = "P/1", Tipo_Profesor = "FULL TIME",
                    Inicio = "01/03/2024", Termino = "30/06/2024", Programa = "MBA",
                    Modalidad = "SUNRISE", Promocion = "2024", nom_catedra = "Gestión Estratégica",
                    TipoCatedra = "Cátedra", Tipo_Horario = "Diurno", AQPQ = "AQ",
                    FacultyQualificationSA_PA_SP_IP = "SA", Confirmado = "Confirmado",
                    HorasDedicadas = 4.5, Alumnos_Totales = 20, Alumnos_Postgrado = 15,
                    Alumnos_Pregrado = 5, Promedio_Ev_Docente = "6,25",
                    Observaciones_Curso = "Observación",
                    Usuario_Observ = "usuario 01-03-2024 10:00",
                },
            ]);
        }
    }
}
