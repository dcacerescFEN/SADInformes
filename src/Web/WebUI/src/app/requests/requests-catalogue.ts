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
    slug: 'non-printed-seminar-homologation-magister',
    title: 'Solicitudes no impresas · Homologación de Seminario X Electivos Magister',
    description: 'Solicitudes no impresas de homologación de seminario por electivos de Magister.',
    legacyPath: 'Informes/Solicitudes/SolicitudesXEstado.aspx?carrera=ic&estado=no impresa&tipo=65',
    available: true,
    permission: 'REPORT_VIEW_REQUESTS',
  },
  {
    slug: 'pending-requests',
    title: 'Solicitudes pendientes',
    description: 'Solicitudes pendientes de tramitación por carrera.',
    legacyPath: 'Informes/Solicitudes/SolicitudesXEstado.aspx?carrera=ic&estado=pendiente',
    available: true,
    permission: 'REPORT_VIEW_REQUESTS',
  },
  {
    slug: 'non-printed-other-types',
    title: 'Solicitudes no impresas · Otros Tipos',
    description: 'Solicitudes no impresas de otros tipos distintos de 67 y 65.',
    legacyPath: 'Informes/Solicitudes/SolicitudesXEstado.aspx?carrera=ic&estado=no impresa&tipo=otros',
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
