import { REPORT_SECTIONS } from '../core/layout/report-sections';
import { migrateFavoriteSlugs } from '../core/layout/reports-sidebar';
import { routes } from '../app.routes';
import { OTHER_REPORTS } from './others-catalogue';

describe('Catálogo de Otros', () => {
  it('registra Cuadro de Honor en Otros, no en Aplicación de reglamento', () => {
    expect(OTHER_REPORTS).toHaveLength(1);
    expect(OTHER_REPORTS[0]).toMatchObject({
      slug: 'honor-roll',
      title: 'Cuadro de Honor',
      legacyPath: 'Informes/AplicacionReglamento/CuadroHonor.aspx',
      available: true,
      permission: 'REPORT_VIEW_HONOR_ROLL',
    });
  });

  it('publica el informe en la sección Otros con ruta propia', () => {
    const section = REPORT_SECTIONS.find(item => item.id === 'otros');
    expect(section?.title).toBe('Otros');
    expect(section?.routePrefix).toBe('/others');
    expect(section?.reports).toBe(OTHER_REPORTS);
  });

  it('incluye el informe en las opciones que el sidebar conoce', () => {
    expect(migrateFavoriteSlugs(OTHER_REPORTS.map(report => report.slug)))
      .toEqual(['honor-roll']);
  });

  it('declara la ruta perezosa de Cuadro de Honor bajo Otros', async () => {
    const route = routes.find(item => item.path === 'others/honor-roll');
    expect(route).toBeDefined();
    expect(route?.canActivate).toHaveLength(1);
    expect(typeof route?.loadComponent).toBe('function');
    const module = await import('./honor-roll/honor-roll');
    expect(typeof module.HonorRoll).toBe('function');
  });
});
