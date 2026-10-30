using Application.Common.Interfaces;
using Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValidities;
using Application.Reports.Postgraduate.TeacherValidities.Queries.GetTeacherValiditiesFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class TeacherValiditiesTests
{
    [Fact]
    public async Task Filters_expose_only_valid_four_digit_years()
    {
        var result = await new GetTeacherValiditiesFiltersHandler(
                new Procedures(), new ApiResponseService())
            .Handle(new GetTeacherValiditiesFilters(), default);
        Assert.Equal(new[] { "2026", "2024" }, result.Data.Years);
    }

    [Fact]
    public async Task Report_preserves_order_and_all_fourteen_fields()
    {
        var procedures = new Procedures();
        var response = await new GetTeacherValiditiesHandler(
                procedures, new ApiResponseService())
            .Handle(new GetTeacherValidities("2024"), default);
        Assert.Equal(new[] { "2024" }, procedures.Arguments);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("2", "2024", "2024F", "MBA600", "01", "Gestión", "MBA", "SUNRISE",
            "2024", "P/1", "Ana Pérez", "ana@fen.uchile.cl", "4.5", true),
            (row.Id, row.Year, row.Period, row.CourseCode, row.Section, row.CourseName,
                row.StudentType, row.Modality, row.Promotion, row.PersonCode, row.Name,
                row.Email, row.DedicatedHours, row.Valid));
        Assert.Equal(1, response.Data.Total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Todos")]
    [InlineData("%")]
    [InlineData("202")]
    [InlineData("2024A")]
    [InlineData("2025")]
    public async Task Invalid_or_out_of_catalog_year_never_calls_the_procedure(string? year)
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetTeacherValiditiesHandler(procedures, new ApiResponseService())
                .Handle(new GetTeacherValidities(year), default));
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_result_is_valid()
    {
        var response = await new GetTeacherValiditiesHandler(
                new Procedures { Empty = true }, new ApiResponseService())
            .Handle(new GetTeacherValidities("2024"), default);
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

        public override Task<List<SpRecProfesoresVigenciasResult>?> SpRec_ProfesoresVigenciasAsync(
            string? año, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Arguments = [año];
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecProfesoresVigenciasResult>?>(Empty ? null :
            [
                new()
                {
                    ID = 2, Año = "2024", Periodo = "2024F", Cod_Catedra = "MBA600",
                    Cod_Seccion = "01", Nom_Catedra = "Gestión", Tipo_Alumno = "MBA",
                    Modalidad = "SUNRISE", Promocion = "2024", Cod_Persona = "P/1",
                    Nombre = "Ana Pérez", Mail1 = "ana@fen.uchile.cl", HorasDedicadas = 4.5,
                    Vigencia = true,
                },
            ]);
        }
    }
}
