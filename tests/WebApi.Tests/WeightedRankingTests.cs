using Application.Common.Interfaces;
using Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRanking;
using Application.Reports.Postgraduate.WeightedRanking.Queries.GetWeightedRankingFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class WeightedRankingTests
{
    [Fact]
    public async Task Filters_remove_Todos_and_wildcards_and_depend_on_selected_type()
    {
        var procedures = new Procedures();
        var result = await new GetWeightedRankingFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetWeightedRankingFilters(null), default);
        Assert.Equal(new[] { "MBA", "MGP" }, result.Data.StudentTypes.Select(row => row.Value));
        Assert.Equal(new[] { "PT", "FT" }, result.Data.Modalities);
        Assert.Equal(new[] { "2026", "2024" }, result.Data.Years);
        Assert.Equal("MBA", procedures.ModalityType);
        await new GetWeightedRankingFiltersHandler(procedures, new ApiResponseService())
            .Handle(new GetWeightedRankingFilters("MGP"), default);
        Assert.Equal("MGP", procedures.ModalityType);
    }

    [Fact]
    public async Task Report_keeps_SP_order_positions_precision_nulls_and_exact_arguments()
    {
        var procedures = new Procedures();
        var response = await new GetWeightedRankingHandler(procedures, new ApiResponseService())
            .Handle(new GetWeightedRanking("MBA", "PT", "2024", "2026"), default);
        Assert.Equal(new[] { "MBA", "PT", "2024", "2026" }, procedures.Arguments);
        Assert.Equal(new[] { 2, 1 }, response.Data.Rows.Select(row => row.Position));
        var row = response.Data.Rows[0];
        Assert.Equal(("Ana Pérez", "00123456-K", "A/1", "2024A", "TITULADO", "5.250", "0.5000"),
            (row.Name, row.Rut, row.StudentCode, row.EntrySemester, row.Situation,
                row.WeightedAverage, row.Percentile));
        Assert.Equal("", response.Data.Rows[1].WeightedAverage);
        Assert.Equal("", response.Data.Rows[1].Percentile);
        Assert.Equal(2, response.Data.Total);
    }

    [Theory]
    [InlineData("Todos", "PT", "2024", "2026")]
    [InlineData("MBA", "Todos", "2024", "2026")]
    [InlineData("MBA", "%", "2024", "2026")]
    [InlineData("MBA", "PT", "Todos", "2026")]
    [InlineData("MBA", "PT", "2024", "Todos")]
    [InlineData("MBA", "PT", "202", "2026")]
    [InlineData("MBA", "PT", "2024", "2026A")]
    [InlineData("OTHER", "PT", "2024", "2026")]
    [InlineData("MBA", "OTHER", "2024", "2026")]
    [InlineData("MBA", "PT", "2025", "2026")]
    [InlineData("MBA", "PT", "2024", "2025")]
    public async Task Invalid_or_out_of_catalog_selection_never_calls_the_ranking(
        string type, string mode, string cohort, string year)
    {
        var procedures = new Procedures();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetWeightedRankingHandler(procedures, new ApiResponseService())
                .Handle(new GetWeightedRanking(type, mode, cohort, year), default));
        Assert.Null(procedures.Arguments);
    }

    [Fact]
    public async Task Empty_SP_result_is_valid_and_does_not_fabricate_ranks()
    {
        var response = await new GetWeightedRankingHandler(
                new Procedures { Empty = true }, new ApiResponseService())
            .Handle(new GetWeightedRanking("MBA", "PT", "2024", "2026"), default);
        Assert.Empty(response.Data.Rows);
        Assert.Equal(0, response.Data.Total);
    }

    internal sealed class Procedures() : SadContextProcedures(new SadContext(new DbContextOptions<SadContext>()))
    {
        public string?[]? Arguments { get; private set; }
        public string? ModalityType { get; private set; }
        public bool Empty { get; init; }
        public bool Fail { get; init; }

        public override Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(
            string? incluirTodos, string? usuario, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("NO", ""), (incluirTodos, usuario));
            return Task.FromResult<List<SpRecTiposAlumnoResult>?>([
                new() { Tipo = "Todos", Nombre = "Todos" },
                new() { Tipo = "MBA", Nombre = "Magíster MBA" },
                new() { Tipo = "MGP", Nombre = "MGP" },
                new() { Tipo = "%", Nombre = "Todos" },
            ]);
        }

        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(
            string? tipo, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            ModalityType = tipo;
            return Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>([
                new() { Modalidad = "Todos" }, new() { Modalidad = "PT" }, new() { Modalidad = "FT" },
            ]);
        }

        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("NO", "Todos"), (incluirTodos, tipoAlumno));
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([
                new() { Año = "Todos" }, new() { Año = "2026" }, new() { Año = "2024" },
            ]);
        }

        public override Task<List<SpRecAlumnosCuadroDeHonorPosgradoResult>?> SpRec_AlumnosCuadroDeHonorPosgradoAsync(
            string? tipo_Alumno, string? modalidad, string? añoIngreso, string? año,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            Arguments = [tipo_Alumno, modalidad, añoIngreso, año];
            if (Fail) throw new InvalidOperationException("private-sql divide by zero");
            return Task.FromResult<List<SpRecAlumnosCuadroDeHonorPosgradoResult>?>(Empty ? null :
            [
                new() { Posicion = 2, Nombre = "Ana Pérez", Rut = "00123456-K", Codigo = "A/1",
                    Ingreso = "2024A", Situacion = "TITULADO", Promedio = 5.250m, Percentil = .5000m },
                new() { Posicion = 1, Nombre = "=FORMULA<>&", Codigo = "A2", Rut = "2-K",
                    Ingreso = "2024A", Situacion = "EGRESADO" },
            ]);
        }
    }
}
