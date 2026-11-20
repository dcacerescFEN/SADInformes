import { PostgraduateReport } from '../postgraduate/postgraduate-catalogue';

export const REGULATION_REPORTS: readonly PostgraduateReport[] = [
  {
    slug: 'graduates-over-3-years',
    title: 'Detección de Alumnos Egresados Más de 3 Años',
    description: 'Alumnos egresados hace más de tres años con su situación académica, decreto y promoción.',
    legacyPath: 'Informes/AplicacionReglamento/EgresadosMas3.aspx',
    available: true,
    permission: 'REPORT_VIEW_REGULATION',
  },
];
