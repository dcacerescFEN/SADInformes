import { PostgraduateReport } from '../postgraduate/postgraduate-catalogue';

export const OTHER_REPORTS: readonly PostgraduateReport[] = [
  {
    slug: 'honor-roll',
    title: 'Cuadro de Honor',
    description: 'Alumnos destacados por carrera y año de ingreso, con su promedio y percentil hasta el período consultado.',
    legacyPath: 'Informes/AplicacionReglamento/CuadroHonor.aspx',
    available: true,
    permission: 'REPORT_VIEW_HONOR_ROLL',
  },
];
