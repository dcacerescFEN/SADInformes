using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.DocenciaWeb.Sp;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

public sealed class DocenciaWebContextProcedures(DocenciaWebContext context) : IDocenciaWebProcedures
{
    public async Task<List<SpRecFECatedrasResult>?> SpRec_FE_CatedrasAsync(
        string? codPersona, string? periodo,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = System.Data.ParameterDirection.Output, SqlDbType = System.Data.SqlDbType.Int };
        var sqlParameters = new object[]
        {
            parameterreturnValue,
            new SqlParameter { ParameterName = "Cod_Persona", Size = 20, Value = codPersona ?? (object)DBNull.Value, SqlDbType = System.Data.SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = System.Data.SqlDbType.VarChar },
        };
        var result = await context.SqlQueryAsync<SpRecFECatedrasResult>(
            "EXEC @returnValue = [dbo].[spRec_FE_Catedras] @Cod_Persona = @Cod_Persona, @Periodo = @Periodo",
            sqlParameters, cancellationToken ?? default, commandTimeoutSeconds: 300);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public async Task<List<SpRecFEModulosResult>?> SpRec_FE_ModulosAsync(
        int? tipoModulo,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = System.Data.ParameterDirection.Output, SqlDbType = System.Data.SqlDbType.Int };
        var sqlParameters = new object[]
        {
            parameterreturnValue,
            new SqlParameter { ParameterName = "TipoModulo", Value = tipoModulo ?? (object)DBNull.Value, SqlDbType = System.Data.SqlDbType.Int },
        };
        var result = await context.SqlQueryAsync<SpRecFEModulosResult>(
            "EXEC @returnValue = [dbo].[spRec_FE_Modulos] @TipoModulo = @TipoModulo",
            sqlParameters, cancellationToken ?? default, commandTimeoutSeconds: 300);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }
}
