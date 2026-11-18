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
  {
    slug: 'non-printed-postgraduate-course-recognition',
    title: 'Solicitudes no impresas · Reconocimiento de Cátedras Aprobadas en Postgrado',
    description: 'Solicitudes no impresas de reconocimiento de cátedras aprobadas en Postgrado.',
    legacyPath: 'Informes/Solicitudes/SolicitudesXEstado.aspx?carrera=ic&estado=no impresa&tipo=67',
    available: true,
    permission: 'REPORT_VIEW_REQUESTS',
  },
];
