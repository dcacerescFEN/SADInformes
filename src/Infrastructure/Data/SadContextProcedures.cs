using System.Data;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

/// <summary>
/// Implementación de los procedimientos del SAD con parámetros tipados, siguiendo
/// la forma del contexto generado de SADAlumnos y SADAdministracion.
/// </summary>
public class SadContextProcedures(SadContext context) : ISadContextProcedures
{
    public virtual async Task<List<SpRecAlumnosXPeriodoXTipoAlumnoXModalidadResult>?> SpRec_AlumnosXPeriodoXTipo_AlumnoXModalidadAsync(
        string? periodo, string? tipo_Alumno, string? modalidad,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            parameterreturnValue,
            new SqlParameter { ParameterName = "Periodo", Size = 20, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 30, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
        };
        var result = await context.SqlQueryAsync<SpRecAlumnosXPeriodoXTipoAlumnoXModalidadResult>(
            "EXEC @returnValue = [dbo].[spRec_AlumnosXPeriodoXTipo_AlumnoXModalidad] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaDocenteXCursoResult>?> SpRec_Encuesta_DocenteXCursoAsync(
        string? periodo, string? tipo_Alumno, string? modalidad,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            parameterreturnValue,
            new SqlParameter { ParameterName = "Periodo", Size = 20, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 30, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaDocenteXCursoResult>(
            "EXEC @returnValue = [dbo].[spRec_Encuesta_DocenteXCurso] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
        string? limite, int? modo, string? conTodos, string? tipo,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            parameterreturnValue,
            new SqlParameter { ParameterName = "Limite", Size = 20, Value = limite ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modo", Value = modo ?? (object)DBNull.Value, SqlDbType = SqlDbType.Int },
            new SqlParameter { ParameterName = "ConTodos", Size = 2, Value = conTodos ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo", Size = 100, Value = tipo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
        };
        var result = await context.SqlQueryAsync<SpRecPeriodosAcademicosProgramacionResult>(
            "EXEC @returnValue = [dbo].[spRec_PeriodosAcademicosProgramacion] @Limite = @Limite, @Modo = @Modo, @ConTodos = @ConTodos, @Tipo = @Tipo",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(
        string? incluirTodos, string? usuario,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            parameterreturnValue,
            new SqlParameter { ParameterName = "IncluirTodos", Size = 2, Value = incluirTodos ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Usuario", Size = 20, Value = usuario ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
        };
        var result = await context.SqlQueryAsync<SpRecTiposAlumnoResult>(
            "EXEC @returnValue = [dbo].[spRec_TiposAlumno] @IncluirTodos = @IncluirTodos, @Usuario = @Usuario",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(
        string? tipo, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            parameterreturnValue,
            new SqlParameter { ParameterName = "Tipo", Size = 30, Value = tipo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
        };
        var result = await context.SqlQueryAsync<SpRecTiposAlumnosModalidadesResult>(
            "EXEC @returnValue = [dbo].[spRec_TiposAlumnosModalidades] @Tipo = @Tipo",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }
}
