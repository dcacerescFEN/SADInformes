export interface PostgraduateReport {
  readonly slug: string;
  readonly title: string;
  readonly description: string;
  readonly legacyPath: string;
  /** true cuando la pantalla ya está migrada y no usa la ficha pendiente. */
  readonly available?: boolean;
}

function entry(slug: string, title: string, description: string, page: string,
  available = false): PostgraduateReport {
  return available
    ? { slug, title, description, legacyPath: 'Informes/Postgrado/' + page, available: true }
    : { slug, title, description, legacyPath: 'Informes/Postgrado/' + page };
}

// Visual inventory only: these entries do not grant access to business operations.
export const POSTGRADUATE_REPORTS: readonly PostgraduateReport[] = [
  entry('encuesta-docente-por-curso', 'Encuesta docente por curso',
    'Resultados de la evaluación docente por período, programa y modalidad.', 'EncuestaDocenteXCurso.aspx', true),
  entry('pago-derecho-basico-anual', 'Pago derecho básico anual',
    'Consulta y registro del pago anual de derecho básico de alumnos.', 'PagoDerechoBasicoAnual.aspx'),
  entry('cursos-por-alumnos', 'Cursos vs. alumnos',
    'Relación de alumnos y cursos por semestre de ingreso, programa y modalidad.', 'CursosXAlumnos.aspx', true),
  entry('inscripcion-masiva', 'Inscripción masiva',
    'Selección de alumnos para inscripción administrativa en cursos.', 'InscripcionMasiva.aspx', true),
  entry('alumnos-sin-contestar-encuesta', 'Alumnos sin contestar encuesta docente',
    'Listado de alumnos con evaluación docente pendiente.', 'AlumnosSinContestarEncuesta.aspx', true),
  entry('cursos-profesores', 'Cursos profesores Postgrado',
    'Cursos y evaluación de profesores por período y departamento.', 'CursosProfesoresPostgrado.aspx', true),
  entry('solicitudes-alumnos', 'Solicitudes alumno',
    'Informe de solicitudes de alumnos por programa, modalidad, ingreso y estado.', 'SolicitudesAlumnosPostgrado.aspx'),
  entry('mantenedores', 'Mantenedores',
    'Acceso a mantenedores de constancias, coordinadores, directores y NCE.', 'Mantenedores.aspx'),
  entry('actualizacion-datos-sad-vti', 'Actualización datos SAD–VTI',
    'Auditoría de actualización de datos de alumnos por fecha y estado.', 'ActulizacionDatosSADVTI.aspx'),
  entry('ranking-titulados-egresados', 'Ranking alumnos titulados/egresados',
    'Ranking académico de alumnos titulados y egresados.', 'RankingTituladosEgresados.aspx'),
  entry('inscripcion-encuesta-salida', 'Inscripción encuesta salida',
    'Selección e inscripción de alumnos en la encuesta de salida.', 'InscripcionEncuestaSalida.aspx'),
  entry('estadisticas-encuesta-salida', 'Estadísticas encuesta salida',
    'Resultados, dimensiones y gráficos de la encuesta de graduados.', 'EncuestaSalidaGraduados/Default.aspx'),
  entry('ranking-notas-ponderado', 'Ranking notas ponderado',
    'Ranking de alumnos graduados según sus notas ponderadas.', 'RankingAlumnosGraduados.aspx'),
  entry('alumnos-matriculados', 'Alumnos matriculados MBA',
    'Alumnos matriculados por año de ingreso y modalidad.', 'AlumnosMatriculados.aspx'),
  entry('profesores-vigencias', 'Profesores vigencias',
    'Informe de vigencias de profesores de Postgrado.', 'ProfesoresVigencias.aspx'),
  entry('alumnos-por-area', 'Alumnos por área',
    'Informe de alumnos y cursos por área académica.', 'ReporteAreasCatedras.aspx'),
  entry('vista-sad-sigma', 'Reporte vista SAD Sigma',
    'Cruce de información de cursos y pagos de profesores con Sigma.', 'ReporteVistaSadSigma.aspx'),
  entry('defensas-tesis', 'Defensas de tesis Postgrado',
    'Reportes de profesores de defensas de tesis por programa, modalidad y año.', 'ReporteProfesoresNuevo.aspx'),
  entry('ayudantias-sigma', 'Ayudantías Postgrado Sigma',
    'Cruce de ayudantías con remuneraciones y estados de Sigma.', 'ReporteAyudantiaSigma.aspx'),
  entry('cambio-firma', 'Cambio firma Postgrado',
    'Administración de firmas, imágenes y excepciones de firmantes.', 'CambioFirmaPostgrado.aspx'),
  entry('estadisticas-encuesta-entrada', 'Estadísticas encuesta entrada',
    'Resultados de la encuesta de entrada de alumnos de Postgrado.', 'EstadisticaEncuestaEntrada.aspx'),
  entry('evaluacion-docente-curso', 'Evaluación docente/curso',
    'Resultados de evaluación docente y cursos según modalidad y año.', 'Eva_CursosProfesores.aspx'),
  entry('datos-personales', 'Datos personales',
    'Informe de datos personales de alumnos de Postgrado.', 'DatosPersonalesPostgrado.aspx'),
  entry('registro-tesis', 'Registro de tesis',
    'Informe de tesis, taller AFE, profesores, notas y exámenes.', 'RegistroTesisPostgrado.aspx'),
  entry('estadisticas-iss', 'Estadísticas ISS',
    'Muestra, categorías y observaciones de la evaluación ISS.', 'EstadisticasISS/Resumen.aspx'),
  entry('estadisticas-encuesta-alumni', 'Estadísticas encuesta Alumni',
    'Resultados de la encuesta Alumni de Postgrado.', 'EstadisticaEncuestaAlumni.aspx'),
  entry('avances-afe', 'Avances AFE',
    'Seguimiento de tareas, grupos y entregas de alumnos en AFE.', 'ReporteAFE.aspx'),
  entry('proximas-licenciaturas-pregrado', 'Próximas licenciaturas Pregrado',
    'Alumnos que cumplen requisitos de licenciatura; opción ubicada en Postgrado del SAD.', 'LicenciaturaPregrado.aspx'),
  entry('notificaciones-causal-eliminacion', 'Notificaciones causal de eliminación',
    'Informe de notificaciones de causal de eliminación de Postgrado.', 'InformeNce.aspx'),
  entry('actividades-cursos', 'Actividades cursos Postgrado',
    'Reporte de actividades de cursos de Postgrado por año.', 'ActividadesCursosPostgrado.aspx'),
  {
    slug: 'programacion-docente-programa-modalidad',
    title: 'Programación Docente Postgrado X Programa y Modalidad',
    description: 'Programación de cursos, profesores y clases por programa, modalidad, promoción y período.',
    legacyPath: 'Informes/Inscripcion/ProgramacionXProgramaModalidad.aspx',
    available: true,
  },
];
