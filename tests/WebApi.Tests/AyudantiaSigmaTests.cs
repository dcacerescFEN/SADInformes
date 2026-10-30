using Application.Common.Interfaces;
using Application.Reports.Postgraduate.AyudantiaSigma.Queries.GetAyudantiaSigma;
using Application.Reports.Postgraduate.AyudantiaSigma.Queries.GetAyudantiaSigmaFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Domain.Entities.SigmaRemuneraciones;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebApi.Tests;

public sealed class AyudantiaSigmaTests
{
    [Fact]
    public async Task Filters_expose_valid_years()
    {
        var result = await new GetAyudantiaSigmaFiltersHandler(
                new Procedures(), new ApiResponseService())
            .Handle(new GetAyudantiaSigmaFilters(), default);
        Assert.Equal(new[] { "2026", "2024" }, result.Data.Years);
    }

    [Fact]
    public async Task Query_maps_twenty_sad_columns_and_five_sigma_columns()
    {
        var (sad, sigma) = CreateContexts();
        await using var _ = sad;
        await using var __ = sigma;
        var response = await new GetAyudantiaSigmaHandler(
                sad, sigma, new Procedures(), new ApiResponseService())
            .Handle(new GetAyudantiaSigma("2024"), default);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("COD-1", "Ana", "María", "Pérez", "González", "P/1",
            "ana@fen.uchile.cl", "MBA600", "Gestión", "01", "01/03/2024", "30/06/2024",
            "2024", "2024F", "MBA", "PT", "2024", "Centro", "AYUDANTIA", "1000",
            "SI", "01/03/2024 10:30:00", "PASO 2", "🚩", "10"),
            (row.Cod, row.Nombre1, row.Nombre2, row.Apellido1, row.Apellido2, row.Rut,
                row.Email, row.CourseCode, row.CourseName, row.Section, row.Start, row.End,
                row.Year, row.Period, row.StudentType, row.Modality, row.Promotion,
                row.CostCenter, row.PaymentType, row.Amount, row.ExistsInSigma, row.SigmaDate,
                row.Step, row.Flag, row.RemunerationId));
    }

    [Fact]
    public async Task Missing_sigma_configuration_keeps_sad_rows_with_NO_fallback()
    {
        var (sad, sigma) = CreateContexts(configured: false);
        await using var _ = sad;
        await using var __ = sigma;
        var response = await new GetAyudantiaSigmaHandler(
                sad, sigma, new Procedures(), new ApiResponseService())
            .Handle(new GetAyudantiaSigma("2024"), default);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal("NO", row.ExistsInSigma);
        Assert.Equal("", row.SigmaDate);
        Assert.Equal("", row.Step);
        Assert.Equal("", row.Flag);
        Assert.Equal("", row.RemunerationId);
    }

    [Fact]
    public async Task Empty_view_is_valid()
    {
        var sadd = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("ayudantia-empty-" + Guid.NewGuid()).Options);
        await using var sad = sadd;
        var sigma = CreateSigma();
        await using var __ = sigma;
        var response = await new GetAyudantiaSigmaHandler(
                sad, sigma, new Procedures(), new ApiResponseService())
            .Handle(new GetAyudantiaSigma("2024"), default);
        Assert.Empty(response.Data.Rows);
        Assert.Equal(0, response.Data.Total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Todos")]
    [InlineData("202")]
    [InlineData("2025")]
    public async Task Invalid_year_never_queries_the_view(string? year)
    {
        var (sad, sigma) = CreateContexts();
        await using var _ = sad;
        await using var __ = sigma;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetAyudantiaSigmaHandler(sad, sigma, new Procedures(), new ApiResponseService())
                .Handle(new GetAyudantiaSigma(year), default));
    }

    internal static (SadContext Sad, SigmaRemuneracionesContext Sigma) CreateContexts(bool configured = true)
    {
        var sad = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("ayudantia-" + Guid.NewGuid()).Options);
        sad.PostgradoAyudantesSigmaSinHistorial.Add(new PostgradoAyudanteSigmaSinHistorial
        {
            COD = "COD-1", Nombre1 = "Ana", Nombre2 = "María", Apellido1 = "Pérez",
            Apellido2 = "González", CodPersona = "P/1", Email = "ana@fen.uchile.cl",
            CodCatedra = "MBA600", NomCatedra = "Gestión", CodSeccion = "01",
            Inicio = new DateOnly(2024, 3, 1), Termino = new DateOnly(2024, 6, 30),
            AñoCatedra = "2024", Periodo = "2024F", TipoAlumnoPostgrado = "MBA",
            ModalidadPostgrado = "PT", Promocion = "2024", Ccosto = "Centro",
            TipoPago = "AYUDANTIA", ValorCursoReal = 1000,
        });
        sad.SaveChanges();
        return (sad, CreateSigma(configured));
    }

    private static SigmaRemuneracionesContext CreateSigma(bool configured = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configured
                ? new Dictionary<string, string?> { ["ConnectionStrings:SigmaRemuneraciones"] = "in-memory" }
                : new Dictionary<string, string?>())
            .Build();
        var sigma = new SigmaRemuneracionesContext(
            new DbContextOptionsBuilder<SigmaRemuneracionesContext>()
                .UseInMemoryDatabase("sigma-ayudantia-" + Guid.NewGuid()).Options, configuration);
        sigma.Remuneraciones.Add(new Remuneracion
        {
            Id = 10, CreatedAt = new DateTime(2024, 3, 1, 10, 30, 0),
            EstadoId = 2.9f, BanderaId = true,
        });
        sigma.Prestaciones.Add(new Prestacion { Id = 1, RemuneracionId = 10, CodImp = "COD-1" });
        sigma.SaveChanges();
        return sigma;
    }

    internal sealed class Procedures() : SadContextProcedures(
        new SadContext(new DbContextOptions<SadContext>()))
    {
        public override Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(
            string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default)
        {
            Assert.Equal(("NO", "Todos"), (incluirTodos, tipoAlumno));
            return Task.FromResult<List<SpRecEnrollmentYearsResult>?>([
                new() { Año = "Todos" }, new() { Año = "2026" }, new() { Año = "2024" },
            ]);
        }
    }
}
