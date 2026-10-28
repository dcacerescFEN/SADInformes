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

    public virtual Task<List<SpRecEnrollmentYearsResult>> SpRec_AnosAsync(CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentYearsResult>(
            "EXEC dbo.spRec_Años @IncluirTodos = @include, @TipoAlumno = @type",
            [Text("include", "NO", 4), Text("type", "Todos", 30)], ct);

    public virtual Task<List<SpRecEnrollmentPeriodsResult>> SpRec_PeriodosAcademicosFiltradosAsync(string type, CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentPeriodsResult>(
            "EXEC dbo.spRec_PeriodosAcademicosFiltrados @Propiedad = @property, @Filtro = @filter, @Orden = @order",
            [Text("property", "Tipo_Alumno", 20), Text("filter", type, 100), Text("order", "DESCENDENTE", 20)], ct);

    public virtual Task<List<SpRecEnrollmentCoursesResult>> SpRec_CursosxPeriodoAsync(
        string period, string type, string modality, CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentCoursesResult>(
            "EXEC dbo.spRec_CursosxPeriodo @Periodo = @period, @Tipo_Alumno = @type, @Modalidad = @modality",
            [Text("period", period, 5), Text("type", type, 30), Text("modality", modality, 30)], ct);

    public virtual Task<List<SpRecEnrollmentCandidatesResult>> SpRec_Alumnos_insc_masiva_catedra_dispAsync(
        string type, string modality, string cohort, string course, CancellationToken ct) =>
        context.SqlQueryAsync<SpRecEnrollmentCandidatesResult>(
            "EXEC dbo.spRec_Alumnos_insc_masiva_catedra_disp @Tipo_Alumno = @type, @Modalidad = @modality, @AñoIngreso = @cohort, @Cod_Catedra = @course",
            [Text("type", type, 30), Text("modality", modality, 30), Text("cohort", cohort, 4), Text("course", course, 30)], ct);


    public virtual Task<int> SpCre_MovInscAdministrativo_PostgradoAsync(
        string codAlumno, string periodo, string codCatedra, string codSeccion, string accion, string pc,
        string extracurricular, string mencionCurso, string tipoCatedra, int eliminaAcademico,
        int autorizadoPorAlumno, string nota, string situacion, string observacionesNota, int registroEspecial,
        string usuario, string? calificadorElectivo, string? tipoCatedraSeleccionable, string sistemaProcesa,
        CancellationToken ct) =>
        context.Database.ExecuteSqlRawAsync(
            """
            EXEC dbo.spCre_MovInscAdministrativo_Postgrado
              @Cod_Alumno=@student, @Periodo=@period, @Cod_Catedra=@course, @Cod_Seccion=@section,
              @Accion=@action, @Pc=@pc, @Extracurricular=@extra, @MencionCurso=@mention, @Tipo_Catedra=@type,
              @EliminaAcademico=@removeAcademic, @AutorizadoPorAlumno=@authorized, @Nota=@grade, @Situacion=@situation,
              @ObservacionesNota=@notes, @RegistroEspecial=@special, @Usuario=@user, @CalificadorElectivo=@elective,
              @TipoCatedraSeleccionable=@selectableType, @SistemaProcesa=@system
            """,
            BuildMovInscAdministrativoParameters(codAlumno, periodo, codCatedra, codSeccion, accion, pc,
                extracurricular, mencionCurso, tipoCatedra, eliminaAcademico, autorizadoPorAlumno, nota,
                situacion, observacionesNota, registroEspecial, usuario, calificadorElectivo,
                tipoCatedraSeleccionable, sistemaProcesa),
            ct);

    /// <summary>Exact parameter contract of the administrative enrollment procedure.</summary>
    public static SqlParameter[] BuildMovInscAdministrativoParameters(
        string codAlumno, string periodo, string codCatedra, string codSeccion, string accion, string pc,
        string extracurricular, string mencionCurso, string tipoCatedra, int eliminaAcademico,
        int autorizadoPorAlumno, string nota, string situacion, string observacionesNota, int registroEspecial,
        string usuario, string? calificadorElectivo, string? tipoCatedraSeleccionable, string sistemaProcesa) =>
        [Text("student", codAlumno, 20), Text("period", periodo, 20), Text("course", codCatedra, 30),
         Text("section", codSeccion, 2), Text("action", accion, 10), Text("pc", pc, 100),
         Text("extra", extracurricular, 2), Text("mention", mencionCurso, 2), Text("type", tipoCatedra, 2),
         Integer("removeAcademic", eliminaAcademico), Integer("authorized", autorizadoPorAlumno),
         Text("grade", nota, 5), Text("situation", situacion, 10), Text("notes", observacionesNota, 1000),
         Integer("special", registroEspecial), Text("user", usuario, 30), Text("elective", calificadorElectivo, 10),
         Text("selectableType", tipoCatedraSeleccionable, 1), Text("system", sistemaProcesa, 15)];

    private static SqlParameter Text(string name, string? value, int size) =>
        new("@" + name, SqlDbType.VarChar, size) { Value = value ?? (object)DBNull.Value };

    private static SqlParameter Integer(string name, int value) =>
        new("@" + name, SqlDbType.Int) { Value = value };
}
