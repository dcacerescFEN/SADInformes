using Application.Common.Interfaces;
using Application.Reports.Requests.UnresolvedPermanence.Queries.GetUnresolvedPermanence;
using Application.Reports.Requests.UnresolvedPermanence.Queries.GetUnresolvedPermanenceFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class UnresolvedPermanenceTests
{
    [Fact]
    public async Task Report_maps_main_row_and_nested_permanence_data()
    {
        var sad = CreateSadContext();
        var procedures = new Procedures();
        var response = await new GetUnresolvedPermanenceHandler(
            sad, procedures, new ApiResponseService()).Handle(
            new GetUnresolvedPermanence("IC"), default);

        Assert.Equal(("20242", "IC"), (response.Data.Period, response.Data.Career));
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("P1", "A1", "ANA PÉREZ", "REGULAR"),
            (row.PersonCode, row.StudentCode, row.Name, row.Situation));
        Assert.Contains("Tipo: NUEVO", row.EntryData);
        Assert.Contains("50 (20242)", row.Credit);
        Assert.Contains("UDA: 30", row.Performance);
        Assert.Contains("CORE1 (2)", row.FailedCore);
        Assert.True(row.PreviousRequests[0].Approved);
        Assert.Equal("Aprobada", row.PreviousRequests[0].Resolution);
        Assert.Equal("20242", row.CurrentRequests[0].Period);
        Assert.Equal("Bajo UD", row.EliminationCauses[0].Text);
    }

    [Fact]
    public async Task Filters_return_periods_and_careers()
    {
        var response = await new GetUnresolvedPermanenceFiltersHandler(
            new ApiResponseService()).Handle(new GetUnresolvedPermanenceFilters(), default);

        Assert.Equal("Ingeniería Comercial", response.Data.Careers[0].Text);
    }

    private static SadContext CreateSadContext()
    {
        var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("unresolved-permanence-sad-" + Guid.NewGuid()).Options);
        context.PeriodosAcademicos.Add(new PeriodoAcademico
        {
            Periodo = "20242", Tipo = "SEMESTRAL",
            InicioPeriodo = DateTime.Now.AddDays(-1), FinPeriodo = DateTime.Now.AddDays(1),
        });
        context.SaveChanges();
        return context;
    }

    private sealed class Procedures : SadContextProcedures
    {
        public Procedures() : base(new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("unresolved-permanence-" + Guid.NewGuid()).Options)) { }

        public override Task<List<SpRecPeriodosAcademicosResult>?> SpRec_PeriodosAcademicosAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecPeriodosAcademicosResult>?>([
                new() { Periodo = "20242", PeriodoLargo = "2024 Primer Semestre" },
            ]);

        public override Task<List<SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult>?> SpRec_AlumnosConSolicitudesPermanenciaNoResueltasAsync(
            string? tipoAlumno, string? periodo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAlumnosConSolicitudesPermanenciaNoResueltasResult>?>([
                new()
                {
                    Cod_Persona = "P1", Cod_Alumno = "A1", Nombre = "ANA PÉREZ",
                    Situacion = "REGULAR", Tipo_Ingreso = "NUEVO", TipoColegio = "MUNICIPAL",
                    Region = "RM", PorcentajeCredito = 50, PeriodoCredito = "20242",
                    UDA = 30, UDR = 2, PorcentajeUDR = 6.7, PromedioAproREP = 5.2m,
                    PeriodoCausalLargo = "2024 Primer Semestre", UdInscritasPeriodoCausal = 30,
                    UdInscritas = 30, SemestresOrdinariosCursados = 2,
                    SemestresExtraordinariosCursados = 0, SemestresOrdinariosNoCursados = 0,
                    BajoUdSemestre = "Bajo UD",
                },
            ]);

        public override Task<List<SpRecAlumnoSolicitudesResult>?> SpRec_AlumnoSolicitudesAsync(
            string? codAlumno, string? filtro,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAlumnoSolicitudesResult>?>(filtro == "PERMANENCIA RESUELTAS"
                ? [new() { PeriodoLargo = "20241", Resolucion = "Aprobada", Autoridad = "Autoridad", TipoDescriptivo = "Permanencia" }]
                : [new() { PeriodoLargo = "20242", TipoDescriptivo = "Permanencia" }]);

        public override Task<List<SpRecAlumnoReprobadasCoreResult>?> SpRec_AlumnoReprobadasCoreAsync(
            string? codAlumno, OutputParameter<int>? returnValue = null,
            CancellationToken? cancellationToken = default) =>
            Task.FromResult<List<SpRecAlumnoReprobadasCoreResult>?>([
                new() { Cod_Catedra = "CORE1", Nom_Catedra = "CORE1", Repitencias = 2 },
            ]);
    }
}
