/**
 * Glosa de la encuesta docente por curso, transcripta de
 * `SAD/Informes/Postgrado/EncuestaDocenteXCurso.aspx` para no alterar el
 * significado de cada pregunta ni de la escala de respuestas.
 */
export interface GlossaryEntry { code: string; description: string; }

export const COURSE_QUESTIONS: readonly GlossaryEntry[] = [
  { code: 'A1', description: 'El curso es un aporte para mi ejercicio profesional' },
  { code: 'A2', description: 'El curso me ha permitido generar una visión diferente del área o de la tarea gerencial' },
  { code: 'A3', description: 'La bibliografía es relevante' },
  { code: 'A4', description: 'Los temas tratados son concordantes con el programa del curso' },
  { code: 'A_Comentarios', description: 'Comentarios de la Evaluación General del Curso' },
];

export const TEACHER_QUESTIONS: readonly GlossaryEntry[] = [
  { code: 'B1', description: 'Demuestra seguridad y dominio sobre las materias tratadas' },
  { code: 'B2', description: 'Ud. tiene una buena opinión general respecto al profesor' },
  { code: 'B3', description: 'Es claro para exponer las materias' },
  { code: 'B4', description: 'Resuelve dudas y problemas de los alumnos' },
  { code: 'B5', description: 'Estimula interés en las materias tratadas' },
  { code: 'B6', description: 'Estimula la discusión en clases' },
  { code: 'B7', description: 'Hace evaluaciones justas y razonables' },
  { code: 'B8', description: 'Entrega retroalimentación oportuna' },
  { code: 'B9', description: 'Entrega retroalimentación detallada' },
  { code: 'B10', description: 'Cumple plazos y normas establecidas' },
  { code: 'B11', description: 'Tiene buena asistencia y es puntual' },
  { code: 'B12', description: 'Genera instancias para integrar conocimientos con la experiencia práctica' },
  { code: 'B13', description: 'Muestra equilibrio entre aspectos teóricos y prácticos' },
  { code: 'B_Nota', description: 'Nota general del profesor' },
  { code: 'B_Comentarios', description: 'Comentarios de la Evaluación del Profesor' },
];

/** Escala usada por las respuestas A1..A4 y B1..B13. */
export const ANSWER_SCALE: readonly GlossaryEntry[] = [
  { code: '1', description: 'Totalmente en desacuerdo' },
  { code: '2', description: 'Muy en desacuerdo' },
  { code: '3', description: 'En desacuerdo' },
  { code: '4', description: 'Ni de acuerdo ni en desacuerdo' },
  { code: '5', description: 'De acuerdo' },
  { code: '6', description: 'Muy de acuerdo' },
  { code: '7', description: 'Totalmente de acuerdo' },
];
