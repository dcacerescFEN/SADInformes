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
    loadComponent: () => import('./postgraduate/postgraduate-placeholder')
      .then(module => module.PostgraduatePlaceholder),
  })),
  { path: '**', redirectTo: '' },
];
