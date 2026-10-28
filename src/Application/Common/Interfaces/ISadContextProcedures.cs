using Domain.Common;
using Domain.Entities.Sad.Sp;

namespace Application.Common.Interfaces;

/// <summary>Procedimientos del SAD consumidos por los informes, al estilo de SADAlumnos.</summary>
public partial interface ISadContextProcedures
{
    Task<List<SpRecAlumnosXPeriodoXTipoAlumnoXModalidadResult>?> SpRec_AlumnosXPeriodoXTipo_AlumnoXModalidadAsync(
        string? periodo, string? tipo_Alumno, string? modalidad,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEncuestaDocenteXCursoResult>?> SpRec_Encuesta_DocenteXCursoAsync(
        string? periodo, string? tipo_Alumno, string? modalidad,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
        string? limite, int? modo, string? conTodos, string? tipo,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(
        string? incluirTodos, string? usuario,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(
        string? tipo, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEnrollmentCandidatesResult>?> SpRec_Alumnos_insc_masiva_catedra_dispAsync(string? tipo_Alumno, string? modalidad, string? añoIngreso, string? cod_Catedra, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEnrollmentYearsResult>?> SpRec_AnosAsync(string? incluirTodos, string? tipoAlumno, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEnrollmentCoursesResult>?> SpRec_CursosxPeriodoAsync(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEnrollmentPeriodsResult>?> SpRec_PeriodosAcademicosFiltradosAsync(string? propiedad, string? filtro, string? orden, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<int> SpCre_MovInscAdministrativo_PostgradoAsync(string? cod_Alumno, string? periodo, string? cod_Catedra, string? cod_Seccion, string? accion, string? pc, string? extracurricular, string? mencionCurso, string? tipo_Catedra, int? eliminaAcademico, int? autorizadoPorAlumno, string? nota, string? situacion, string? observacionesNota, int? registroEspecial, string? usuario, string? calificadorElectivo, string? tipoCatedraSeleccionable, string? sistemaProcesa, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
}
