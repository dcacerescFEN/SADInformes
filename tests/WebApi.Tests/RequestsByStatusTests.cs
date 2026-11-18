using Application.Common.Interfaces;
using Application.Reports.Requests.RequestsByStatus.Queries.GetRequestsByStatus;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class RequestsByStatusTests
{
    [Fact]
    public async Task Report_defaults_to_type_67_and_maps_the_legacy_row()
    {
        var procedures = new Procedures();
        var response = await new GetRequestsByStatusHandler(
            procedures, new ApiResponseService()).Handle(
            new GetRequestsByStatus("IC", "NO IMPRESA", null), default);

        Assert.Equal("67", procedures.LastType);
        var row = Assert.Single(response.Data.Rows);
        Assert.Equal(("P1", "A1", "20242/00001", "15/03/2024"),
            (row.PersonCode, row.StudentCode, row.RequestCode, row.Date));
        Assert.Equal(("Reconocimiento de Cátedras Aprobadas en Postgrado", "REGULAR", "Comentario"),
            (row.Type, row.Situation, row.Comment));
    }

    [Fact]
    public async Task Report_uses_type_65_for_the_seminar_homologation_report()
    {
        var procedures = new Procedures();
        await new GetRequestsByStatusHandler(procedures, new ApiResponseService())
            .Handle(new GetRequestsByStatus("IICG", "NO IMPRESA", "65"), default);

        Assert.Equal(("IICG", "NO IMPRESA", "65"),
            (procedures.LastCareer, procedures.LastStatus, procedures.LastType));
    }

    private sealed class Procedures : SadContextProcedures
    {
        public Procedures() : base(new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseInMemoryDatabase("requests-status-" + Guid.NewGuid()).Options)) { }

        public string? LastCareer { get; private set; }
        public string? LastStatus { get; private set; }
        public string? LastType { get; private set; }

        public override Task<List<SpRecSolicitudesXEstadoTipoResult>?> SpRec_SolicitudesXEstado_TipoAsync(
            string? carrera, string? estado, string? tipoSolicitud,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            LastCareer = carrera;
            LastStatus = estado;
            LastType = tipoSolicitud;
            return Task.FromResult<List<SpRecSolicitudesXEstadoTipoResult>?>([
                new() { Cod_Tipo_Solicitud = "67", Tipo_Solicitud = "Reconocimiento de Cátedras Aprobadas en Postgrado" },
                new() { Cod_Tipo_Solicitud = "65", Tipo_Solicitud = "Homologación de Seminario X Electivos Magister" },
            ]);
        }

        public override Task<List<SpRecSolicitudesXEstadoResult>?> SpRec_SolicitudesXEstadoAsync(
            string? carrera, string? estado, string? tipoSolicitud,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            LastCareer = carrera;
            LastStatus = estado;
            LastType = tipoSolicitud;
            return Task.FromResult<List<SpRecSolicitudesXEstadoResult>?>([
                new()
                {
                    Cod_Persona = "P1", Cod_Alumno = "A1", Nombre = "ANA PÉREZ",
                    Cod_Solicitud = "2024200001", Fecha_Solicitud = new DateTime(2024, 3, 15),
                    Tipo_Solicitud = "Reconocimiento de Cátedras Aprobadas en Postgrado",
                    Situacion = "REGULAR", ComentarioValidacion = "Comentario",
                    Primero = "20242/00001", Ultimo = "20242/00001",
                },
            ]);
        }
    }
}
