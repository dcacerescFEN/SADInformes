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

export const LEGACY_POSTGRADUATE_SLUGS = {
  'encuesta-docente-por-curso': 'teacher-survey',
  'pago-derecho-basico-anual': 'basic-right-payment',
  'cursos-por-alumnos': 'courses-by-students',
  'inscripcion-masiva': 'mass-enrollment',
  'alumnos-sin-contestar-encuesta': 'unanswered-teacher-survey',
  'cursos-profesores': 'teacher-courses',
  'solicitudes-alumnos': 'student-requests',
  'actualizacion-datos-sad-vti': 'sad-vti-updates',
  'ranking-titulados-egresados': 'graduate-ranking',
  'inscripcion-encuesta-salida': 'exit-survey-enrollment',
  'estadisticas-encuesta-salida': 'exit-survey-statistics',
  'ranking-notas-ponderado': 'weighted-ranking',
  'alumnos-matriculados': 'enrolled-students',
  'profesores-vigencias': 'teacher-validities',
  'alumnos-por-area': 'area-courses',
  'vista-sad-sigma': 'sad-sigma',
  'defensas-tesis': 'thesis-defenses',
  'ayudantias-sigma': 'sigma-tutorships',
  'estadisticas-encuesta-entrada': 'entry-survey-statistics',
  'evaluacion-docente-curso': 'course-teacher-evaluation',
  'datos-personales': 'personal-data',
  'registro-tesis': 'thesis-registration',
  'estadisticas-iss': 'iss-statistics',
  'estadisticas-encuesta-alumni': 'alumni-survey-statistics',
  'avances-afe': 'afe-report',
  'proximas-licenciaturas-pregrado': 'undergraduate-licenciatura',
  'notificaciones-causal-eliminacion': 'nce',
  'actividades-cursos': 'course-activities',
  'programacion-docente-programa-modalidad': 'teaching-schedule',
} as const;

// Visual inventory only: these entries do not grant access to business operations.
export const POSTGRADUATE_REPORTS: readonly PostgraduateReport[] = [
  entry('teacher-survey', 'Encuesta docente por curso',
    'Resultados de la evaluación docente por período, programa y modalidad.', 'EncuestaDocenteXCurso.aspx', true),
  entry('basic-right-payment', 'Pago derecho básico anual',
    'Consulta y registro del pago anual de derecho básico de alumnos.', 'PagoDerechoBasicoAnual.aspx', true),
  entry('courses-by-students', 'Cursos vs. alumnos',
    'Relación de alumnos y cursos por semestre de ingreso, programa y modalidad.', 'CursosXAlumnos.aspx', true),
  entry('mass-enrollment', 'Inscripción masiva',
    'Selección de alumnos para inscripción administrativa en cursos.', 'InscripcionMasiva.aspx', true),
  entry('unanswered-teacher-survey', 'Alumnos sin contestar encuesta docente',
    'Listado de alumnos con evaluación docente pendiente.', 'AlumnosSinContestarEncuesta.aspx', true),
  entry('teacher-courses', 'Cursos profesores Postgrado',
    'Cursos y evaluación de profesores por período y departamento.', 'CursosProfesoresPostgrado.aspx', true),
  entry('student-requests', 'Solicitudes alumno',
    'Informe de solicitudes de alumnos por programa, modalidad, ingreso y estado.', 'SolicitudesAlumnosPostgrado.aspx', true),
  entry('sad-vti-updates', 'Actualización datos SAD–VTI',
    'Auditoría de actualización de datos de alumnos por fecha y estado.', 'ActulizacionDatosSADVTI.aspx', true),
  entry('graduate-ranking', 'Ranking alumnos titulados/egresados',
    'Ranking académico de alumnos titulados y egresados.', 'RankingTituladosEgresados.aspx'),
  entry('exit-survey-enrollment', 'Inscripción encuesta salida',
    'Selección e inscripción de alumnos en la encuesta de salida.', 'InscripcionEncuestaSalida.aspx', true),
  entry('exit-survey-statistics', 'Estadísticas encuesta salida',
    'Resultados, dimensiones y gráficos de la encuesta de graduados.', 'EncuestaSalidaGraduados/Default.aspx', true),
  entry('weighted-ranking', 'Ranking notas ponderado',
    'Ranking de alumnos graduados según sus notas ponderadas.', 'RankingAlumnosGraduados.aspx', true),
  entry('enrolled-students', 'Alumnos matriculados MBA',
    'Alumnos matriculados por año de ingreso y modalidad.', 'AlumnosMatriculados.aspx', true),
  entry('teacher-validities', 'Profesores vigencias',
    'Informe de vigencias de profesores de Postgrado.', 'ProfesoresVigencias.aspx', true),
  entry('area-courses', 'Alumnos por área',
    'Informe de alumnos y cursos por área académica.', 'ReporteAreasCatedras.aspx', true),
  entry('sad-sigma', 'Reporte vista SAD Sigma',
    'Cruce de información de cursos y pagos de profesores con Sigma.', 'ReporteVistaSadSigma.aspx', true),
  entry('thesis-defenses', 'Defensas de tesis Postgrado',
    'Reportes de profesores de defensas de tesis por programa, modalidad y año.', 'ReporteProfesoresNuevo.aspx', true),
  entry('sigma-tutorships', 'Ayudantías Postgrado Sigma',
    'Cruce de ayudantías con remuneraciones y estados de Sigma.', 'ReporteAyudantiaSigma.aspx', true),
  entry('entry-survey-statistics', 'Estadísticas encuesta entrada',
    'Resultados de la encuesta de entrada de alumnos de Postgrado.', 'EstadisticaEncuestaEntrada.aspx', true),
  entry('course-teacher-evaluation', 'Evaluación docente/curso',
    'Resultados de evaluación docente y cursos según modalidad y año.', 'Eva_CursosProfesores.aspx', true),
  entry('personal-data', 'Datos personales',
    'Informe de datos personales de alumnos de Postgrado.', 'DatosPersonalesPostgrado.aspx', true),
  entry('thesis-registration', 'Registro de tesis',
    'Informe de tesis, taller AFE, profesores, notas y exámenes.', 'RegistroTesisPostgrado.aspx', true),
  entry('iss-statistics', 'Estadísticas ISS',
    'Muestra, categorías y observaciones de la evaluación ISS.', 'EstadisticasISS/Resumen.aspx', true),
  entry('alumni-survey-statistics', 'Estadísticas encuesta Alumni',
    'Resultados de la encuesta Alumni de Postgrado.', 'EstadisticaEncuestaAlumni.aspx', true),
  entry('afe-report', 'Avances AFE',
    'Seguimiento de tareas, grupos y entregas de alumnos en AFE.', 'ReporteAFE.aspx', true),
  entry('undergraduate-licenciatura', 'Próximas licenciaturas Pregrado',
    'Alumnos que cumplen requisitos de licenciatura; opción ubicada en Postgrado del SAD.', 'LicenciaturaPregrado.aspx', true),
  entry('nce', 'Notificaciones causal de eliminación',
    'Informe de notificaciones de causal de eliminación de Postgrado.', 'InformeNce.aspx', true),
  entry('course-activities', 'Actividades cursos Postgrado',
    'Reporte de actividades de cursos de Postgrado por año.', 'ActividadesCursosPostgrado.aspx', true),
  {
    slug: 'teaching-schedule',
    title: 'Programación Docente Postgrado X Programa y Modalidad',
    description: 'Programación de cursos, profesores y clases por programa, modalidad, promoción y período.',
    legacyPath: 'Informes/Inscripcion/ProgramacionXProgramaModalidad.aspx',
    available: true,
  },
];
