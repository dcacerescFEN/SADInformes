using Domain.Common;
using Domain.Entities.Sad.Sp;

namespace Application.Common.Interfaces;

/// <summary>Procedimientos del SAD consumidos por los informes, al estilo de SADAlumnos.</summary>
public partial interface ISadContextProcedures
{
    Task<List<SpRecInformePostgradoProfesoresGCIResult>?> SpRec_InformePostgradoProfesoresGCIAsync(
        string? anioDefensa, string? tipoAlumno, string? modalidad, string? anioIngreso,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecInformePostgradoProfesoresGCI2Result>?> SpRec_InformePostgradoProfesoresGCI2Async(
        string? anioDefensa, string? tipoAlumno, string? modalidad, string? anioIngreso,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecTipoAlumnosPostgradoResult>?> SpRecTipoAlumnosPostgradoAsync(
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecModalidadesPostgradoResult>?> SpRecModalidadesPostgradoAsync(
        string? tipoAlumno, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecCatedrasAreasXPagosProfesoresResult>?> SpRec_CatedrasAreasXPagosProfesoresAsync(
        string? año, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecCatedrasAreasReportePostgradoResult>?> SpRec_CatedrasAreas_ReportePostgradoAsync(
        string? año1, string? año2,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecProfesoresVigenciasResult>?> SpRec_ProfesoresVigenciasAsync(
        string? año, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecAlumnosMatriculadosPostgradoResult>?> SpRec_AlumnosMatriculadosPostgradoAsync(
        string? modalidad, string? anio,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecAlumnosCuadroDeHonorPosgradoResult>?> SpRec_AlumnosCuadroDeHonorPosgradoAsync(
        string? tipo_Alumno, string? modalidad, string? añoIngreso, string? año,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEncuestaGraduadosTiposAlumnoResult>?> spRec_EncuestaGraduados_TiposAlumnoAsync(
        string? incluirTodos, string? usuario, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosAlumnosResult>?> spRec_EncuestaGraduados_AlumnosAsync(
        string? tipo_Alumno, string? modalidad, string? año, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<int> spCre_EncuestaGraduados_Ins_AlumnoAsync(
        string? cod_Alumno, string? periodo, string? fecha_Egreso, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ17Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIAsync(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIIAsync(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosProfesoresResult>?> spRec_EncuestaGraduadosEstadisticasProfesoresIIIAsync(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosInsatisfaccionResult>?> spRec_EncuestaGraduadosEstadisticasInsatisfaccionAsync(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosDetalleProfResult>?> spRec_EncuestaGraduadosEstadisticasDetalleProfAsync(string? periodo, string? tipo_Alumno, string? modalidad, string? profesor, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEncuestaGraduadosQ8Result>?> spRec_EncuestaGraduadosEstadisticasQ8Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosQ11Result>?> spRec_EncuestaGraduadosEstadisticasQ11Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ1Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ2Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ3Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ4Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ5Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ6Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosLikertResult>?> spRec_EncuestaGraduadosEstadisticasQ7Async(string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEncuestaGraduadosPeriodosResult>?> spRec_EncuestaGraduados_PeriodosAsync(
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosEstadisticasResult>?> spRec_EncuestaGraduados_EstadisticasAsync(
        string? periodo, string? tipo_Alumno, string? modalidad, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
    Task<List<SpRecEncuestaGraduadosModalidadesResult>?> spRec_EncuestaGraduados_ModalidadesAsync(
        string? tipo, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

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

    Task<List<SpRecAlumnosSinEvaluacionDocenteResult>?> SpRec_Alumnos_Sin_Evaluacion_DocenteAsync(
        string? periodo, string? tipo_Alumno, string? modalidad,
        OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecPeriodosAcademicosXTipoResult>?> SpRec_PeriodosAcademicosXTipoAsync(string? tipo, string? escuela, string? limite, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecProgramacionDocenteXProgramaModalidadResult>?> SpRec_ProgramacionDocenteXProgramaModalidadAsync(string? periodo, string? tipoPeriodo, string? tipo_Alumno, string? modalidad, string? promocion, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecPromocionesPostgradoTodosResult>?> SpRec_PromocionesPostgradoTodosAsync(OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecTiposAlumnoXEscuelaResult>?> SpRec_TiposAlumnoXEscuelaAsync(string? escuela, string? incluirTodos, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecDocentesDepartamentosResult>?> SpRec_DocentesDepartamentosAsync(string? incluirTodos, string? usuario, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);

    Task<List<SpRecEvaPostgradoCursosDocentes2Result>?> SpRec_EvaPostgradoCursosDocentes2Async(string? año, string? departamento, OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default);
}
