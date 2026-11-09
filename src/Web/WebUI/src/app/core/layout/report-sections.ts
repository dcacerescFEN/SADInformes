import { POSTGRADUATE_REPORTS, PostgraduateReport } from '../../postgraduate/postgraduate-catalogue';
import { ACADEMIC_ENROLLMENT_REPORTS } from '../../academic-enrollment/academic-enrollment-catalogue';

export interface ReportSection {
  readonly id: string;
  readonly title: string;
  readonly icon: string;
  readonly routePrefix?: string;
  readonly reports: readonly PostgraduateReport[];
}

// Section names and order follow SAD/Informes/default.aspx; Postgrado and Inscripción académica have local routes.
export const REPORT_SECTIONS: readonly ReportSection[] = [
  { id: 'academic-enrollment', title: 'Inscripción académica', icon: 'bi-journal-check', routePrefix: '/academic-enrollment', reports: ACADEMIC_ENROLLMENT_REPORTS },
  { id: 'solicitudes', title: 'Solicitudes', icon: 'bi-inbox', reports: [] },
  { id: 'aplicacion-reglamento', title: 'Aplicación de reglamento', icon: 'bi-journal-text', reports: [] },
  { id: 'certificados', title: 'Certificados', icon: 'bi-file-earmark-check', reports: [] },
  { id: 'bienestar', title: 'Bienestar', icon: 'bi-heart', reports: [] },
  { id: 'ceremonia', title: 'Ceremonia', icon: 'bi-award', reports: [] },
  { id: 'acreditacion', title: 'Acreditación', icon: 'bi-patch-check', reports: [] },
  { id: 'postgrado', title: 'Postgrado', icon: 'bi-mortarboard', routePrefix: '/postgraduate', reports: POSTGRADUATE_REPORTS },
  { id: 'gestion-docente', title: 'Gestión docente', icon: 'bi-person-workspace', reports: [] },
  { id: 'cadfpreg', title: 'Cadfpreg', icon: 'bi-clipboard-data', reports: [] },
  { id: 'otros', title: 'Otros', icon: 'bi-three-dots', reports: [] },
  { id: 'nexocomunicacion', title: 'NexoComunicacion', icon: 'bi-chat-dots', reports: [] },
  { id: 'see', title: 'SEE', icon: 'bi-calendar-week', reports: [] },
  { id: 'hibridas-extracurriculares', title: 'Híbridas y actividades extracurriculares', icon: 'bi-calendar-event', reports: [] },
];
