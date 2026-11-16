using Application.Common.Interfaces;
using Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingFilters;
using Application.Reports.Requests.AutomaticProcessingRequests.Queries.GetAutomaticProcessingRequests;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class AutomaticProcessingRequestsTests
{
    [Fact]
    public async Task Filters_return_request_periods_and_normalized_states()
    {
        await using var context = CreateContext();
        var response = await new GetAutomaticProcessingFiltersHandler(
            context, new ApiResponseService()).Handle(new GetAutomaticProcessingFilters(), default);

        Assert.Equal(["20242", "20241"], response.Data.Periods);
        Assert.Equal(("", "TODAS"), (response.Data.States[0].Value, response.Data.States[0].Text));
        Assert.Contains(response.Data.States, state => state.Value == "SIN DATO");
        Assert.Contains(response.Data.States, state => state.Value == "ACEPTADA");
        Assert.Equal("Ingeniería Comercial", response.Data.Careers.Single(career => career.Value == "IC").Text);
    }

    [Fact]
    public async Task Report_passes_selected_filters_and_maps_legacy_columns()
    {
        await using var context = CreateContext();
        var procedures = new Procedures(context);
        var response = await new GetAutomaticProcessingRequestsHandler(
            context, procedures, new ApiResponseService()).Handle(
            new GetAutomaticProcessingRequests("20242", "IC", "", "ACEPTADA"), default);

        Assert.Equal(("IC", "20242", "", "ACEPTADA"),
            (procedures.LastCareer, procedures.LastPeriod, procedures.LastType, procedures.LastStatus));
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("ANA PÉREZ", "A1", "20242/00001", "15/03/2024", "Diferir"),
            (row.Name, row.StudentCode, row.RequestCode, row.Date, row.Type));
        Assert.Equal(("Cursar 30 UD", "APROBADA", "20/03/2024"),
            (row.Requested, row.AdministrativeProcessing, row.ProcessDate));
    }

    private static SadContext CreateContext()
    {
        var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("automatic-requests-" + Guid.NewGuid()).Options);
        context.Solicitudes89.AddRange(
            new Solicitud89
            {
                CodSolicitud = "2024200001", CodAlumno = "A1", TipoSolicitud = "2",
                PeriodoSolicitud = "20242", TramitacionAutomatizada = "ACEPTADA",
            },
            new Solicitud89
            {
                CodSolicitud = "2024100001", CodAlumno = "A2", TipoSolicitud = "8",
                PeriodoSolicitud = "20241", TramitacionAutomatizada = null,
            });
        context.SaveChanges();
        return context;
    }

    private sealed class Procedures(SadContext context) : SadContextProcedures(context)
    {
        public string? LastCareer { get; private set; }
        public string? LastPeriod { get; private set; }
        public string? LastType { get; private set; }
        public string? LastStatus { get; private set; }

        public override Task<List<SpRecSolicitudesProcesadasAutomaticamenteSadResult>?> SpRec_SolicitudesProcesadasAutomaticamenteSadAsync(
            string? carrera, string? periodo, string? tipoSol, string? tramitacionAutomatizada,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            LastCareer = carrera;
            LastPeriod = periodo;
            LastType = tipoSol;
            LastStatus = tramitacionAutomatizada;
            return Task.FromResult<List<SpRecSolicitudesProcesadasAutomaticamenteSadResult>?>([
                new()
                {
                    Nombre = "ANA PÉREZ", Cod_Alumno = "A1", Cod_Solicitud = "20242/00001",
                    Fecha_Solicitud = new DateTime(2024, 3, 15), Tipo = "Diferir",
                    Solicitado = "Cursar 30 UD", Tramitacion_Administrativa = "APROBADA",
                    Resultado = "Aceptada", FechaAut = new DateTime(2024, 3, 20),
                },
            ]);
        }
    }
}
