using Application.Common.Interfaces;
using Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudents;
using Application.Reports.Postgraduate.EnrolledStudents.Queries.GetEnrolledStudentsFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class EnrolledStudentsTests
{
    [Fact]
    public async Task Filters_expose_only_the_two_legacy_modalities_and_valid_years()
    {
        var result = await new GetEnrolledStudentsFiltersHandler(
                new Procedures(), new ApiResponseService())
            .Handle(new GetEnrolledStudentsFilters(), default);
        Assert.Equal(new[] { "FOR THE AMERICAS", "SUNRISE" },
            result.Data.Modalities.Select(row => row.Value));
        Assert.Equal(new[] { "2026", "2024" }, result.Data.Years);
    }

    [Fact]
    public async Task Report_maps_all_fifteen_columns_and_exact_arguments()
    {
        var procedures = new Procedures();
        var response = await new GetEnrolledStudentsHandler(
                procedures, new ApiResponseService())
            .Handle(new GetEnrolledStudents("SUNRISE", "2024"), default);
        Assert.Equal(new[] { "SUNRISE", "2024" }, procedures.Arguments);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("A/1", "2024F", "00123456-K", "MBA", "SUNRISE", "REGULAR",
            "Ana", "Pérez", "ana@example.test", "ana@fen.uchile.cl", "ana@work.test",
            "+56911111111", "+56222222222", "Empresa", "Cargo"),
            (row.StudentCode, row.EntrySemester, row.Rut, row.StudentType, row.Modality,
                row.AcademicSituation, row.Names, row.Surnames, row.PersonalEmail,
                row.FenEmail, row.WorkEmail, row.Mobile, row.WorkPhone, row.Company,
                row.Position));
        Assert.Equal(1, response.Data.Total);
    }

    [Theory]
    [InlineData("Todos", "2024")]
    [InlineData("%", "2024")]
    [InlineData("OTHER", "2024")]
    [InlineData("SUNRISE", "Todos")]
    [InlineData("SUNRISE", "202")]
    [InlineData("SUNRISE", "2024A")]
    [InlineData("SUNRISE", "2025")]
    public async Task Invalid_or_out_of_catalog_selection_never_calls_the_procedure(
        string modality, string year)
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetEnrolledStudentsHandler(procedures, new ApiResponseService())
                .Handle(new GetEnrolledStudents(modality, year), default));
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_result_is_valid_and_does_not_fabricate_rows()
    {
        var response = await new GetEnrolledStudentsHandler(
                new Procedures { Empty = true }, new ApiResponseService())
            .Handle(new GetEnrolledStudents("FOR THE AMERICAS", "2024"), default);
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

        public override Task<List<SpRecAlumnosMatriculadosPostgradoResult>?> SpRec_AlumnosMatriculadosPostgradoAsync(
            string? modalidad, string? anio, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Arguments = [modalidad, anio];
            if (Fail) throw new InvalidOperationException("private-sql");
            return Task.FromResult<List<SpRecAlumnosMatriculadosPostgradoResult>?>(Empty ? null :
            [
                new()
                {
                    Cod_Alumno = "A/1", Sem_IngresoDecreto = "2024F", Rut = "00123456-K",
                    Tipo_Alumno = "MBA", Modalidad = "SUNRISE", Cod_SitAcademica = "REGULAR",
                    Nombres = "Ana", Apellidos = "Pérez", Email_Personal = "ana@example.test",
                    Email_Fen = "ana@fen.uchile.cl", Email_laboral = "ana@work.test",
                    Celular = "+56911111111", Tel_laboral = "+56222222222",
                    NomEmpresa = "Empresa", CargoEmpresa = "Cargo",
                },
            ]);
        }
    }
}
