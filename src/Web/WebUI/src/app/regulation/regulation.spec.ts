import { REPORT_SECTIONS } from '../core/layout/report-sections';
import { migrateFavoriteSlugs } from '../core/layout/reports-sidebar';
import { routes } from '../app.routes';
import { REGULATION_REPORTS } from './regulation-catalogue';

describe('Catálogo de Aplicación de reglamento', () => {
  it('registra el informe de egresados con su permiso y ruta legacy', () => {
    expect(REGULATION_REPORTS).toHaveLength(4);
    const report = REGULATION_REPORTS[0];
    expect(report).toMatchObject({
      slug: 'graduates-over-3-years',
      title: 'Detección de Alumnos Egresados Más de 3 Años',
      legacyPath: 'Informes/AplicacionReglamento/EgresadosMas3.aspx',
      available: true,
      permission: 'REPORT_VIEW_REGULATION',
    });
    expect(report.description.trim().length).toBeGreaterThan(0);
  });

  it('registra el informe de expedientes no abiertos con su permiso y ruta legacy', () => {
    const report = REGULATION_REPORTS[1];
    expect(report).toMatchObject({
      slug: 'unopened-files',
      title: 'Alumnos que cumplen requisitos y no han abierto expediente',
      legacyPath: 'Informes/AplicacionReglamento/ExpedientesNoAbiertos.aspx',
      available: true,
      permission: 'REPORT_VIEW_REGULATION',
    });
    expect(report.description.trim().length).toBeGreaterThan(0);
  });

  it('registra el informe de expedientes abiertos por período con su permiso y ruta legacy', () => {
    const report = REGULATION_REPORTS[2];
    expect(report).toMatchObject({
      slug: 'opened-files-by-period',
      title: 'Expedientes Abiertos en un Período Dado',
      legacyPath: 'Informes/AplicacionReglamento/ExpedientesXPeriodo.aspx',
      available: true,
      permission: 'REPORT_VIEW_REGULATION',
    });
    expect(report.description.trim().length).toBeGreaterThan(0);
  });

  it('registra el informe de ranking con su permiso y ruta legacy', () => {
    const report = REGULATION_REPORTS[3];
    expect(report).toMatchObject({
      slug: 'ranking',
      title: 'Ranking de Licenciatura y Egreso',
      legacyPath: 'Informes/AplicacionReglamento/Ranking.aspx',
      available: true,
      permission: 'REPORT_VIEW_REGULATION',
    });
    expect(report.description.trim().length).toBeGreaterThan(0);
  });

  it('publica el informe en la sección Aplicación de reglamento con ruta propia', () => {
    const section = REPORT_SECTIONS.find(item => item.id === 'aplicacion-reglamento');
    expect(section?.title).toBe('Aplicación de reglamento');
    expect(section?.routePrefix).toBe('/regulation');
    expect(section?.reports).toBe(REGULATION_REPORTS);
  });

  it('incluye el informe en las opciones que el sidebar conoce', () => {
    expect(migrateFavoriteSlugs(REGULATION_REPORTS.map(report => report.slug)))
      .toEqual(['graduates-over-3-years', 'unopened-files', 'opened-files-by-period', 'ranking']);
  });

  it('declara la ruta perezosa del informe de expedientes abiertos por período', async () => {
    const route = routes.find(item => item.path === 'regulation/opened-files-by-period');
    expect(route).toBeDefined();
    expect(route?.canActivate).toHaveLength(1);
    expect(typeof route?.loadComponent).toBe('function');
    const module = await import('./opened-files-by-period/opened-files-by-period');
    expect(typeof module.OpenedFilesByPeriod).toBe('function');
  });

  it('declara la ruta perezosa del informe de ranking', async () => {
    const route = routes.find(item => item.path === 'regulation/ranking');
    expect(route).toBeDefined();
    expect(route?.canActivate).toHaveLength(1);
    expect(typeof route?.loadComponent).toBe('function');
    const module = await import('./ranking/ranking');
    expect(typeof module.Ranking).toBe('function');
  });

});
