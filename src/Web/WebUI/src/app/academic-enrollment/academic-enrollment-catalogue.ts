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
  {
    slug: 'students-completed-free-courses',
    title: 'Alumnos con libres completados',
    description: 'Alumnos que completaron cursos libres y tienen inscritas de ese tipo.',
    legacyPath: 'Informes/Inscripcion/CompletaronLibres.aspx',
    available: true,
  },
  {
    slug: 'students-completed-electives',
    title: 'Alumnos con electivos completados',
    description: 'Alumnos que completaron electivos y tienen inscritas de ese tipo.',
    legacyPath: 'Informes/Inscripcion/CompletaronElectivos.aspx',
    available: true,
  },
  {
    slug: 'automatic-mention-changes',
    title: 'Cambios automáticos de mención',
    description: 'Alumnos que han sido cambiados de mención automáticamente.',
    legacyPath: 'Informes/Inscripcion/CambiosMencionAutomaticos.aspx',
    available: true,
  },
  {
    slug: 'enrolled-approved-courses',
    title: 'Cátedras inscritas aprobadas',
    description: 'Cátedras inscritas que ya han sido aprobadas.',
    legacyPath: 'Informes/Inscripcion/InscritasYaAprobadas.aspx',
    available: true,
  },
  {
    slug: 'enrolled-without-requirements',
    title: 'Cátedras inscritas sin requisitos',
    description: 'Cátedras inscritas sin requisitos.',
    legacyPath: 'Informes/Inscripcion/InscritasSinRequisitos.aspx',
    available: true,
  },
  {
    slug: 'failed-mandatory-not-enrolled',
    title: 'Obligatorias reprobadas no inscritas',
    description: 'Cátedras obligatorias reprobadas que no están inscritas.',
    legacyPath: 'Informes/Inscripcion/ObligatoriasReprobadasNoInscritas.aspx',
    available: true,
  },
  {
    slug: 'enrollment-teaching-schedule',
    title: 'Programación Docente',
    description: 'Programación docente del proceso de inscripción.',
    legacyPath: 'Informes/Inscripcion/ProgramacionDocenteCompacta.aspx',
    available: true,
  },
  {
    slug: 'teaching-schedule-ic-old-plan',
    title: 'Programación Docente IC Plan Antiguo',
    description: 'Programación docente de Ingeniería Comercial, plan antiguo.',
    legacyPath: 'Informes/Inscripcion/ProgramacionDocenteDecreto.aspx?decreto=00921/89%',
    available: true,
  },
  {
    slug: 'teaching-schedule-ic-new-plan',
    title: 'Programación Docente IC Plan Nuevo',
    description: 'Programación docente de Ingeniería Comercial, plan nuevo.',
    legacyPath: 'Informes/Inscripcion/ProgramacionDocenteDecreto.aspx?decreto=009615/2003%',
    available: true,
  },
  {
    slug: 'teaching-schedule-iicg-old-plan',
    title: 'Programación Docente IICG Plan Antiguo',
    description: 'Programación docente de Ingeniería en Información y Control de Gestión, plan antiguo.',
    legacyPath: 'Informes/Inscripcion/ProgramacionDocenteDecreto.aspx?decreto=00433/89%',
    available: true,
  },
  {
    slug: 'teaching-schedule-iicg-new-plan',
    title: 'Programación Docente IICG Plan Nuevo',
    description: 'Programación docente de Ingeniería en Información y Control de Gestión, plan nuevo.',
    legacyPath: 'Informes/Inscripcion/ProgramacionDocenteDecreto.aspx?decreto=009538/2003%',
    available: true,
  },
  {
    slug: 'course-crossing',
    title: 'Cruce de Cursos',
    description: 'Cruce de cursos para el período seleccionado.',
    legacyPath: 'Informes/Inscripcion/CruceCursos.aspx',
    available: true,
  },
  {
    slug: 'courses-for-room-assignment',
    title: 'Cursos para asignación de salas',
    description: 'Cursos considerados por el sistema de asignación de salas.',
    legacyPath: 'Informes/Inscripcion/CursosSistemaAsignacionSalas.aspx',
    available: true,
  },
  {
    slug: 'students-without-photo',
    title: 'Alumnos sin fotos',
    description: 'Alumnos que no tienen una fotografía registrada.',
    legacyPath: 'Informes/Inscripcion/SinFotos.aspx',
    available: true,
  },
  entry('course-load-reduction', 'Reducción de carga',
    'Alumnos con reducción de carga en el período.', 'ReduccionCarga.aspx'),
];
