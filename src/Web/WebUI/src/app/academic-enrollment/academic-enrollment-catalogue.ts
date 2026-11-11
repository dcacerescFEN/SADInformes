import { PostgraduateReport } from '../postgraduate/postgraduate-catalogue';

// Reuse the report catalogue shape: these options are links to pending local
// fiches, not migrated report screens.
function entry(slug: string, title: string, description: string, page: string,
  routePath?: string): PostgraduateReport {
  const legacyPath = 'Informes/Inscripcion/' + page;
  return routePath
    ? { slug, title, description, legacyPath, routePath }
    : { slug, title, description, legacyPath };
}

export const ACADEMIC_ENROLLMENT_REPORTS: readonly PostgraduateReport[] = [
  {
    slug: 'electives-without-common-plan',
    title: 'Electivos de mención que no están en el plan común',
    description: 'Electivos de mención que no forman parte del plan común.',
    legacyPath: 'Informes/Inscripcion/ElectivosMencionSinPlanComun.aspx',
    available: true,
  },
  {
    slug: 'students-without-priority',
    title: 'Alumnos que pueden postular y no tienen prioridad',
    description: 'Alumnos de IC, IICG y CA que pueden postular y tienen prioridad 0.',
    legacyPath: 'Informes/Inscripcion/AlumnosSinPrioridad.aspx',
    available: true,
  },
  {
    slug: 'postulation-results-by-course',
    title: 'Resultados postulación por curso',
    description: 'Resultados por curso del procesamiento de la postulación.',
    legacyPath: 'Informes/Inscripcion/ResPostulaAgregaXCurso.aspx?origen=INSCRIPCION',
    available: true,
  },
  {
    slug: 'permutation-results-by-student',
    title: 'Resultados permuta por alumno',
    description: 'Resultados por alumno del procesamiento de la permuta.',
    legacyPath: 'Informes/Inscripcion/ResPermutaXAlumno.aspx',
    available: true,
  },
  {
    slug: 'permutation-rejections-by-course',
    title: 'Permutas rechazadas por vacantes',
    description: 'Totales por curso de permutas rechazadas por vacantes.',
    legacyPath: 'Informes/Inscripcion/ResPermutaXCursoVacantes.aspx',
    available: true,
  },
  {
    slug: 'add-results-by-course',
    title: 'Resultados agrega por curso',
    description: 'Resultados por curso del procesamiento del agrega.',
    legacyPath: 'Informes/Inscripcion/ResPostulaAgregaXCurso.aspx?origen=AGREGA',
    available: true,
  },
  entry('students-completed-free-courses', 'Alumnos con libres completados',
    'Alumnos que completaron cursos libres y tienen inscritas de ese tipo.', 'CompletaronLibres.aspx'),
  entry('students-completed-electives', 'Alumnos con electivos completados',
    'Alumnos que completaron electivos y tienen inscritas de ese tipo.', 'CompletaronLibres.aspx'),
  entry('automatic-mention-changes', 'Cambios automáticos de mención',
    'Alumnos que han sido cambiados de mención automáticamente.', 'CambiosMencionAutomaticos.aspx'),
  entry('enrolled-approved-courses', 'Cátedras inscritas aprobadas',
    'Cátedras inscritas que ya han sido aprobadas.', 'InscritasYaAprobadas.aspx'),
  entry('enrolled-without-requirements', 'Cátedras inscritas sin requisitos',
    'Cátedras inscritas sin requisitos.', 'InscritasSinRequisitos.aspx'),
  entry('failed-mandatory-not-enrolled', 'Obligatorias reprobadas no inscritas',
    'Cátedras obligatorias reprobadas que no están inscritas.', 'ObligatoriasReprobadasNoInscritas.aspx'),
  entry('enrollment-teaching-schedule', 'Programación Docente',
    'Programación docente del proceso de inscripción.', 'ProgramacionDocenteCompacta.aspx'),
  entry('teaching-schedule-ic-old-plan', 'Programación Docente IC Plan Antiguo',
    'Programación docente de Ingeniería Comercial, plan antiguo.', 'ProgramacionDocenteDecreto.aspx?decreto=00921/89%'),
  entry('teaching-schedule-ic-new-plan', 'Programación Docente IC Plan Nuevo',
    'Programación docente de Ingeniería Comercial, plan nuevo.', 'ProgramacionDocenteDecreto.aspx?decreto=009615/2003%'),
  entry('teaching-schedule-iicg-old-plan', 'Programación Docente IICG Plan Antiguo',
    'Programación docente de Ingeniería en Información y Control de Gestión, plan antiguo.', 'ProgramacionDocenteDecreto.aspx?decreto=00433/89%'),
  entry('teaching-schedule-iicg-new-plan', 'Programación Docente IICG Plan Nuevo',
    'Programación docente de Ingeniería en Información y Control de Gestión, plan nuevo.', 'ProgramacionDocenteDecreto.aspx?decreto=009538/2003%'),
  entry('course-crossing', 'Cruce de Cursos',
    'Cruce de cursos para el período seleccionado.', 'CruceCursos.aspx'),
  entry('courses-for-room-assignment', 'Cursos para asignación de salas',
    'Cursos considerados por el sistema de asignación de salas.', 'CursosSistemaAsignacionSalas.aspx'),
  entry('postgraduate-teaching-schedule', 'Programación Docente Postgrado',
    'Programación docente de Postgrado por programa y modalidad.',
    'ProgramacionXProgramaModalidad.aspx', '/postgraduate/teaching-schedule'),
  entry('students-without-photo', 'Alumnos sin fotos',
    'Alumnos que no tienen una fotografía registrada.', 'SinFotos.aspx'),
  entry('course-load-reduction', 'Reducción de carga',
    'Alumnos con reducción de carga en el período.', 'ReduccionCarga.aspx'),
];
