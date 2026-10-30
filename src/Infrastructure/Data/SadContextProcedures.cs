using System.Data;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Implementación de los procedimientos del SAD con parámetros tipados, siguiendo
/// la forma del contexto generado de SADAlumnos y SADAdministracion.
/// </summary>
public partial class SadContextProcedures(SadContext context) : ISadContextProcedures
{
    public virtual async Task<List<SpRecProfesoresVigenciasResult>?> SpRec_ProfesoresVigenciasAsync(
        string? año, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Año", Size = 10, Value = año ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecProfesoresVigenciasResult>(
            "EXEC @returnValue = [dbo].[spRec_ProfesoresVigencias] @Año = @Año",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecAlumnosMatriculadosPostgradoResult>?> SpRec_AlumnosMatriculadosPostgradoAsync(
        string? modalidad, string? anio, OutputParameter<int>? returnValue = null,
        CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Modalidad", Size = 30, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Anio", Size = 5, Value = anio ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecAlumnosMatriculadosPostgradoResult>(
            "EXEC @returnValue = [dbo].[spRec_AlumnosMatriculadosPostgrado] @Modalidad = @Modalidad, @Anio = @Anio",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecAlumnosCuadroDeHonorPosgradoResult>?> SpRec_AlumnosCuadroDeHonorPosgradoAsync(
        string? tipo_Alumno, string? modalidad, string? añoIngreso, string? año,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int,
        };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 30, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "AñoIngreso", Size = 4, Value = añoIngreso ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Año", Size = 4, Value = año ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecAlumnosCuadroDeHonorPosgradoResult>(
            "EXEC @returnValue = [dbo].[spRec_AlumnosCuadroDeHonorPosgrado] @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad, @AñoIngreso = @AñoIngreso, @Año = @Año",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosTiposAlumnoResult>?> spRec_EncuestaGraduados_TiposAlumnoAsync(
        string? incluirTodos, string? usuario, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "IncluirTodos", Size = 2, Value = incluirTodos ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Usuario", Size = 20, Value = usuario ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosTiposAlumnoResult>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_TiposAlumno] @IncluirTodos = @IncluirTodos, @Usuario = @Usuario",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosAlumnosResult>?> spRec_EncuestaGraduados_AlumnosAsync(
        string? tipo_Alumno, string? modalidad, string? año, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 30, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Año", Size = 4, Value = año ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosAlumnosResult>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Alumnos] @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad, @Año = @Año",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<int> spCre_EncuestaGraduados_Ins_AlumnoAsync(
        string? cod_Alumno, string? periodo, string? fecha_Egreso, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Cod_Alumno", Size = 30, Value = cod_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Fecha_Egreso", Size = 40, Value = fecha_Egreso ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var affected = await context.Database.ExecuteSqlRawAsync(
            "EXEC @returnValue = [dbo].[spCre_EncuestaGraduados_Ins_Alumno] @Cod_Alumno = @Cod_Alumno, @Periodo = @Periodo, @Fecha_Egreso = @Fecha_Egreso",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return affected;
    }

    public virtual Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIAsync(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteTeachersAsync("I", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIIAsync(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteTeachersAsync("II", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIIIAsync(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteTeachersAsync("III", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);

    private async Task<List<SpRecEncuestaGraduadosProfesoresResult>?> ExecuteTeachersAsync(string question,
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue, CancellationToken? cancellationToken)
    {
        var procedure = question switch
        {
            "I" => "spRec_EncuestaGraduados_Estadisticas_ProfesoresI",
            "II" => "spRec_EncuestaGraduados_Estadisticas_ProfesoresII",
            "III" => "spRec_EncuestaGraduados_Estadisticas_ProfesoresIII",
            _ => throw new ArgumentOutOfRangeException(nameof(question)),
        };
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 10, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosProfesoresResult>(
            $"EXEC @returnValue = [dbo].[{procedure}] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosInsatisfaccionResult>?> spRec_EncuestaGraduadosEstadisticasInsatisfaccionAsync(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 10, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosInsatisfaccionResult>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_EstadisticasInsatisfaccion] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosDetalleProfResult>?> spRec_EncuestaGraduadosEstadisticasDetalleProfAsync(
        string? periodo, string? tipo_Alumno, string? modalidad, string? profesor, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 10, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Profesor", Size = 20, Value = profesor ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosDetalleProfResult>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Estadisticas_DetallesProf] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad, @Profesor = @Profesor",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosQ8Result>?> spRec_EncuestaGraduadosEstadisticasQ8Async(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 10, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosQ8Result>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduadosEstadisticasQ8] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosQ11Result>?> spRec_EncuestaGraduadosEstadisticasQ11Async(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 10, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosQ11Result>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduadosEstadisticasQ11] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ1Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q1", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ2Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q2", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ3Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q3", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ4Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q4", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ5Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q5", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ6Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q6", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ17Async(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q17", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);
    public virtual Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ7Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default) =>
        ExecuteLikertAsync("Q7", periodo, tipo_Alumno, modalidad, returnValue, cancellationToken);

    private async Task<List<SpRecEncuestaGraduadosLikertResult>?> ExecuteLikertAsync(string question,
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue, CancellationToken? cancellationToken)
    {
        var procedure = question switch
        {
            "Q1" => "spRec_EncuestaGraduadosEstadisticasQ1",
            "Q2" => "spRec_EncuestaGraduadosEstadisticasQ2",
            "Q3" => "spRec_EncuestaGraduadosEstadisticasQ3",
            "Q4" => "spRec_EncuestaGraduadosEstadisticasQ4",
            "Q5" => "spRec_EncuestaGraduadosEstadisticasQ5",
            "Q6" => "spRec_EncuestaGraduadosEstadisticasQ6",
            "Q7" => "spRec_EncuestaGraduadosEstadisticasQ7",
            "Q17" => "spRec_EncuestaGraduadosEstadisticasQ17",
            _ => throw new ArgumentOutOfRangeException(nameof(question)),
        };
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 10, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosLikertResult>(
            $"EXEC @returnValue = [dbo].[{procedure}] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosPeriodosResult>?> spRec_EncuestaGraduados_PeriodosAsync(
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosPeriodosResult>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Periodos]", [parameterreturnValue], cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosEstadisticasResult>?> spRec_EncuestaGraduados_EstadisticasAsync(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Periodo", Size = 5, Value = periodo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Tipo_Alumno", Size = 30, Value = tipo_Alumno ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            new SqlParameter { ParameterName = "Modalidad", Size = 10, Value = modalidad ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosEstadisticasResult>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Estadisticas] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad",
            sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

    public virtual async Task<List<SpRecEncuestaGraduadosModalidadesResult>?> spRec_EncuestaGraduados_ModalidadesAsync(
        string? tipo, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter { ParameterName = "returnValue", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
        var sqlParameters = new object[]
        {
            new SqlParameter { ParameterName = "Tipo", Size = 30, Value = tipo ?? (object)DBNull.Value, SqlDbType = SqlDbType.VarChar },
            parameterreturnValue,
        };
        var result = await context.SqlQueryAsync<SpRecEncuestaGraduadosModalidadesResult>(
            "EXEC @returnValue = [dbo].[spRec_EncuestaGraduados_Modalidades] @Tipo = @Tipo", sqlParameters, cancellationToken ?? default);
        returnValue?.SetValue(parameterreturnValue.Value);
        return result;
    }

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

    public virtual async Task<List<SpRecEnrollmentCandidatesResult>?> SpRec_Alumnos_insc_masiva_catedra_dispAsync(string? tipo_Alumno, string? modalidad, string? añoIngreso, string? cod_Catedra, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Tipo_Alumno",
                Size = 30,
                Value = tipo_Alumno ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Modalidad",
                Size = 30,
                Value = modalidad ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "AñoIngreso",
                Size = 4,
                Value = añoIngreso ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Cod_Catedra",
                Size = 30,
                Value = cod_Catedra ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecEnrollmentCandidatesResult>("EXEC @returnValue = [dbo].[spRec_Alumnos_insc_masiva_catedra_disp] @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad, @AñoIngreso = @AñoIngreso, @Cod_Catedra = @Cod_Catedra", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "IncluirTodos",
                Size = 4,
                Value = incluirTodos ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "TipoAlumno",
                Size = 30,
                Value = tipoAlumno ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecEnrollmentYearsResult>("EXEC @returnValue = [dbo].[spRec_Años] @IncluirTodos = @IncluirTodos, @TipoAlumno = @TipoAlumno", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecEnrollmentCoursesResult>?> SpRec_CursosxPeriodoAsync(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Periodo",
                Size = 5,
                Value = periodo ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Tipo_Alumno",
                Size = 30,
                Value = tipo_Alumno ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Modalidad",
                Size = 30,
                Value = modalidad ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecEnrollmentCoursesResult>("EXEC @returnValue = [dbo].[spRec_CursosxPeriodo] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecEnrollmentPeriodsResult>?> SpRec_PeriodosAcademicosFiltradosAsync(string? propiedad, string? filtro, string? orden, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Propiedad",
                Size = 20,
                Value = propiedad ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Filtro",
                Size = 100,
                Value = filtro ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Orden",
                Size = 20,
                Value = orden ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecEnrollmentPeriodsResult>("EXEC @returnValue = [dbo].[spRec_PeriodosAcademicosFiltrados] @Propiedad = @Propiedad, @Filtro = @Filtro, @Orden = @Orden", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<int> SpCre_MovInscAdministrativo_PostgradoAsync(string? cod_Alumno, string? periodo, string? cod_Catedra, string? cod_Seccion, string? accion, string? pc, string? extracurricular, string? mencionCurso, string? tipo_Catedra, int? eliminaAcademico, int? autorizadoPorAlumno, string? nota, string? situacion, string? observacionesNota, int? registroEspecial, string? usuario, string? calificadorElectivo, string? tipoCatedraSeleccionable, string? sistemaProcesa, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = ParameterDirection.Output,
            SqlDbType = SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Cod_Alumno",
                Size = 20,
                Value = cod_Alumno ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Periodo",
                Size = 20,
                Value = periodo ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Cod_Catedra",
                Size = 30,
                Value = cod_Catedra ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Cod_Seccion",
                Size = 2,
                Value = cod_Seccion ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Accion",
                Size = 10,
                Value = accion ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Pc",
                Size = 100,
                Value = pc ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Extracurricular",
                Size = 2,
                Value = extracurricular ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "MencionCurso",
                Size = 2,
                Value = mencionCurso ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Tipo_Catedra",
                Size = 2,
                Value = tipo_Catedra ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "EliminaAcademico",
                Value = eliminaAcademico ?? Convert.DBNull,
                SqlDbType = SqlDbType.Int,
            },
            new SqlParameter
            {
                ParameterName = "AutorizadoPorAlumno",
                Value = autorizadoPorAlumno ?? Convert.DBNull,
                SqlDbType = SqlDbType.Int,
            },
            new SqlParameter
            {
                ParameterName = "Nota",
                Size = 5,
                Value = nota ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Situacion",
                Size = 10,
                Value = situacion ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "ObservacionesNota",
                Size = 1000,
                Value = observacionesNota ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "RegistroEspecial",
                Value = registroEspecial ?? Convert.DBNull,
                SqlDbType = SqlDbType.Int,
            },
            new SqlParameter
            {
                ParameterName = "Usuario",
                Size = 30,
                Value = usuario ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "CalificadorElectivo",
                Size = 10,
                Value = calificadorElectivo ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "TipoCatedraSeleccionable",
                Size = 1,
                Value = tipoCatedraSeleccionable ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "SistemaProcesa",
                Size = 15,
                Value = sistemaProcesa ?? Convert.DBNull,
                SqlDbType = SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.Database.ExecuteSqlRawAsync("EXEC @returnValue = [dbo].[spCre_MovInscAdministrativo_Postgrado] @Cod_Alumno = @Cod_Alumno, @Periodo = @Periodo, @Cod_Catedra = @Cod_Catedra, @Cod_Seccion = @Cod_Seccion, @Accion = @Accion, @Pc = @Pc, @Extracurricular = @Extracurricular, @MencionCurso = @MencionCurso, @Tipo_Catedra = @Tipo_Catedra, @EliminaAcademico = @EliminaAcademico, @AutorizadoPorAlumno = @AutorizadoPorAlumno, @Nota = @Nota, @Situacion = @Situacion, @ObservacionesNota = @ObservacionesNota, @RegistroEspecial = @RegistroEspecial, @Usuario = @Usuario, @CalificadorElectivo = @CalificadorElectivo, @TipoCatedraSeleccionable = @TipoCatedraSeleccionable, @SistemaProcesa = @SistemaProcesa", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecAlumnosSinEvaluacionDocenteResult>?> SpRec_Alumnos_Sin_Evaluacion_DocenteAsync(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Periodo",
                Size = 20,
                Value = periodo ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Tipo_Alumno",
                Size = 30,
                Value = tipo_Alumno ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Modalidad",
                Size = 30,
                Value = modalidad ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecAlumnosSinEvaluacionDocenteResult>("EXEC @returnValue = [dbo].[spRec_Alumnos_Sin_Evaluacion_Docente] @Periodo = @Periodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecPeriodosAcademicosXTipoResult>?> SpRec_PeriodosAcademicosXTipoAsync(string? tipo, string? escuela, string? limite, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Tipo",
                Size = 20,
                Value = tipo ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Escuela",
                Size = 20,
                Value = escuela ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Limite",
                Size = 20,
                Value = limite ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecPeriodosAcademicosXTipoResult>("EXEC @returnValue = [dbo].[spRec_PeriodosAcademicosXTipo] @Tipo = @Tipo, @Escuela = @Escuela, @Limite = @Limite", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecProgramacionDocenteXProgramaModalidadResult>?> SpRec_ProgramacionDocenteXProgramaModalidadAsync(string? periodo, string? tipoPeriodo, string? tipo_Alumno, string? modalidad, string? promocion, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Periodo",
                Size = 20,
                Value = periodo ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "TipoPeriodo",
                Size = 20,
                Value = tipoPeriodo ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Tipo_Alumno",
                Size = 30,
                Value = tipo_Alumno ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Modalidad",
                Size = 30,
                Value = modalidad ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Promocion",
                Size = 7,
                Value = promocion ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecProgramacionDocenteXProgramaModalidadResult>("EXEC @returnValue = [dbo].[spRec_ProgramacionDocenteXProgramaModalidad] @Periodo = @Periodo, @TipoPeriodo = @TipoPeriodo, @Tipo_Alumno = @Tipo_Alumno, @Modalidad = @Modalidad, @Promocion = @Promocion", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecPromocionesPostgradoTodosResult>?> SpRec_PromocionesPostgradoTodosAsync(OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecPromocionesPostgradoTodosResult>("EXEC @returnValue = [dbo].[spRec_PromocionesPostgradoTodos]", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecTiposAlumnoXEscuelaResult>?> SpRec_TiposAlumnoXEscuelaAsync(string? escuela, string? incluirTodos, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Escuela",
                Size = 20,
                Value = escuela ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "IncluirTodos",
                Size = 2,
                Value = incluirTodos ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecTiposAlumnoXEscuelaResult>("EXEC @returnValue = [dbo].[spRec_TiposAlumnoXEscuela] @Escuela = @Escuela, @IncluirTodos = @IncluirTodos", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecDocentesDepartamentosResult>?> SpRec_DocentesDepartamentosAsync(string? incluirTodos, string? usuario, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "IncluirTodos",
                Size = 2,
                Value = incluirTodos ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Usuario",
                Size = 20,
                Value = usuario ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecDocentesDepartamentosResult>("EXEC @returnValue = [dbo].[spRec_DocentesDepartamentos] @IncluirTodos = @IncluirTodos, @Usuario = @Usuario", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }

    public virtual async Task<List<SpRecEvaPostgradoCursosDocentes2Result>?> SpRec_EvaPostgradoCursosDocentes2Async(string? año, string? departamento, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
    {
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        };

        var sqlParameters = new []
        {
            new SqlParameter
            {
                ParameterName = "Año",
                Size = 4,
                Value = año ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            new SqlParameter
            {
                ParameterName = "Departamento",
                Size = 70,
                Value = departamento ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            },
            parameterreturnValue,
        };
        var _ = await context.SqlQueryAsync<SpRecEvaPostgradoCursosDocentes2Result>("EXEC @returnValue = [dbo].[spRec_EvaPostgradoCursosDocentes2] @Año = @Año, @Departamento = @Departamento", sqlParameters, cancellationToken ?? CancellationToken.None);

        returnValue?.SetValue(parameterreturnValue.Value);

        return _;
    }
}
