using Domain.Common;
using Domain.Entities.DocenciaWeb.Sp;

namespace Application.Common.Interfaces;

public interface IDocenciaWebProcedures
{
    Task<List<SpRecFECatedrasResult>?> SpRec_FE_CatedrasAsync(
        string? codPersona, string? periodo,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecFEModulosResult>?> SpRec_FE_ModulosAsync(
        int? tipoModulo,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
}
