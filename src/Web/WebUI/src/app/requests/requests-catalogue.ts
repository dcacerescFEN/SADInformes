import { PostgraduateReport } from '../postgraduate/postgraduate-catalogue';

export const REQUESTS_REPORTS: readonly PostgraduateReport[] = [
  {
    slug: 'automatic-processing-requests',
    title: 'Solicitudes procesadas automáticamente',
    description: 'Solicitudes procesadas automáticamente por carrera, período, tipo y resultado.',
    legacyPath: 'Informes/Solicitudes/SolicitudesProcesadasAutomaticamente.aspx',
    available: true,
    permission: 'REPORT_VIEW_REQUESTS',
  },
];
