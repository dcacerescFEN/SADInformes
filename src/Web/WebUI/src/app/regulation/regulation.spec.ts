import { REPORT_SECTIONS } from '../core/layout/report-sections';
import { migrateFavoriteSlugs } from '../core/layout/reports-sidebar';
import { REGULATION_REPORTS } from './regulation-catalogue';

describe('Catálogo de Aplicación de reglamento', () => {
  it('registra el informe de egresados con su permiso y ruta legacy', () => {
    expect(REGULATION_REPORTS).toHaveLength(1);
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

  it('publica el informe en la sección Aplicación de reglamento con ruta propia', () => {
    const section = REPORT_SECTIONS.find(item => item.id === 'aplicacion-reglamento');
    expect(section?.title).toBe('Aplicación de reglamento');
    expect(section?.routePrefix).toBe('/regulation');
    expect(section?.reports).toBe(REGULATION_REPORTS);
  });

  it('incluye el informe en las opciones que el sidebar conoce', () => {
    expect(migrateFavoriteSlugs(REGULATION_REPORTS.map(report => report.slug)))
      .toEqual(['graduates-over-3-years']);
  });
});
