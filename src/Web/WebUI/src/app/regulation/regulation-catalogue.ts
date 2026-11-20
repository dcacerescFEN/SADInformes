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
  {
    slug: 'unopened-files',
    title: 'Alumnos que cumplen requisitos y no han abierto expediente',
    description: 'Alumnos que cumplen los requisitos de la reglamentación y aún no han abierto su expediente de titulación.',
    legacyPath: 'Informes/AplicacionReglamento/ExpedientesNoAbiertos.aspx',
    available: true,
    permission: 'REPORT_VIEW_REGULATION',
  },
  {
    slug: 'opened-files-by-period',
    title: 'Expedientes Abiertos en un Período Dado',
    description: 'Alumnos que abrieron su expediente de titulación dentro de un período, con su situación académica y datos de contacto.',
    legacyPath: 'Informes/AplicacionReglamento/ExpedientesXPeriodo.aspx',
    available: true,
    permission: 'REPORT_VIEW_REGULATION',
  },
  {
    slug: 'ranking',
    title: 'Ranking de Licenciatura y Egreso',
    description: 'Ranking de alumnos de licenciatura y egreso por grupo, período, tipo de selección, orden y dirección.',
    legacyPath: 'Informes/AplicacionReglamento/Ranking.aspx',
    available: true,
    permission: 'REPORT_VIEW_REGULATION',
  },
];
