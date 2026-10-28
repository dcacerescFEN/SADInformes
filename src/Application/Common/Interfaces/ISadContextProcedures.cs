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

    Task<List<SpRecEnrollmentYearsResult>> SpRec_AnosAsync(CancellationToken ct);
    Task<List<SpRecEnrollmentPeriodsResult>> SpRec_PeriodosAcademicosFiltradosAsync(string type, CancellationToken ct);
    Task<List<SpRecEnrollmentCoursesResult>> SpRec_CursosxPeriodoAsync(string period, string type, string modality, CancellationToken ct);
    Task<List<SpRecEnrollmentCandidatesResult>> SpRec_Alumnos_insc_masiva_catedra_dispAsync(
        string type, string modality, string cohort, string course, CancellationToken ct);

    Task<int> SpCre_MovInscAdministrativo_PostgradoAsync(
        string codAlumno, string periodo, string codCatedra, string codSeccion, string accion, string pc,
        string extracurricular, string mencionCurso, string tipoCatedra, int eliminaAcademico,
        int autorizadoPorAlumno, string nota, string situacion, string observacionesNota, int registroEspecial,
        string usuario, string? calificadorElectivo, string? tipoCatedraSeleccionable, string sistemaProcesa,
        CancellationToken ct);
}
