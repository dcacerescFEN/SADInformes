import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth-guard';
import { POSTGRADUATE_REPORTS } from './postgraduate/postgraduate-catalogue';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [authGuard],
    loadComponent: () => import('./home').then((module) => module.Home),
  },
  ...POSTGRADUATE_REPORTS.map(report => ({
    path: 'postgrado/' + report.slug,
    canActivate: [authGuard],
    data: { report },
    loadComponent: () => report.slug === 'inscripcion-masiva'
      ? import('./postgraduate/mass-enrollment/mass-enrollment').then((module) => module.MassEnrollment)
      : report.slug === 'inscripcion-encuesta-salida'
      ? import('./postgraduate/exit-survey-enrollment/exit-survey-enrollment').then((module) => module.ExitSurveyEnrollment)
      : report.slug === 'estadisticas-encuesta-salida'
      ? import('./postgraduate/exit-survey-statistics/exit-survey-statistics').then((module) => module.ExitSurveyStatistics)
      : report.slug === 'ranking-notas-ponderado'
      ? import('./postgraduate/weighted-ranking/weighted-ranking').then((module) => module.WeightedRanking)
      : report.slug === 'alumnos-matriculados'
      ? import('./postgraduate/enrolled-students/enrolled-students').then((module) => module.EnrolledStudents)
      : report.slug === 'profesores-vigencias'
      ? import('./postgraduate/teacher-validities/teacher-validities').then((module) => module.TeacherValidities)
      : report.slug === 'alumnos-por-area'
      ? import('./postgraduate/area-courses/area-courses').then((module) => module.AreaCourses)
      : report.slug === 'actualizacion-datos-sad-vti'
      ? import('./postgraduate/sad-vti-updates/sad-vti-updates').then((module) => module.SadVtiUpdates)
      : report.slug === 'solicitudes-alumnos'
      ? import('./postgraduate/student-requests/student-requests').then((module) => module.StudentRequests)
      : report.slug === 'cursos-por-alumnos'
      ? import('./postgraduate/courses-by-students/courses-by-students').then((module) => module.CoursesByStudents)
      : report.slug === 'alumnos-sin-contestar-encuesta'
      ? import('./postgraduate/unanswered-teacher-survey/unanswered-teacher-survey').then((module) => module.UnansweredTeacherSurvey)
      : report.slug === 'cursos-profesores'
      ? import('./postgraduate/teacher-courses/teacher-courses').then((module) => module.TeacherCourses)
      : report.slug === 'programacion-docente-programa-modalidad'
      ? import('./postgraduate/teaching-schedule/teaching-schedule').then((module) => module.TeachingSchedule)
      : report.available
        ? import('./postgraduate/teacher-survey/teacher-survey').then((module) => module.TeacherSurvey)
        : import('./postgraduate/postgraduate-placeholder').then((module) => module.PostgraduatePlaceholder),
  })),
  { path: '**', redirectTo: '' },
];
