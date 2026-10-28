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
      : report.slug === 'cursos-por-alumnos'
      ? import('./postgraduate/courses-by-students/courses-by-students').then((module) => module.CoursesByStudents)
      : report.available
        ? import('./postgraduate/teacher-survey/teacher-survey').then((module) => module.TeacherSurvey)
        : import('./postgraduate/postgraduate-placeholder').then((module) => module.PostgraduatePlaceholder),
  })),
  { path: '**', redirectTo: '' },
];
