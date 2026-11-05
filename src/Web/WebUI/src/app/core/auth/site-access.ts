const POLICIES = new Map<string, { module: string; permission?: string }>([
  ['NewSAD', { module: 'SAD', permission: 'SAD_ACCESS' }],
  ['SADAlumnos', { module: 'STUDENT', permission: 'STUDENT_ACCESS_STUDENT_MODULE' }],
  ['SADInformes', { module: 'REPORT', permission: 'REPORT_ACCESS_REPORT_MODULE' }],
  ['SADAdministracion', { module: 'ADMINISTRATION', permission: 'ADMINISTRATION_ACCESS_ADMINISTRATION_MODULE' }],
  ['SADProfesores', { module: 'TEACHER', permission: 'TEACHER_ACCESS_TEACHER_MODULE' }],
  ['SADCursos', { module: 'COURSE', permission: 'COURSE_ACCESS_COURSE_MODULE' }],
  ['SADExamenes', { module: 'EXAM', permission: 'EXAM_ACCESS_EXAM_MODULE' }],
  ['SADActividades', { module: 'ACTIVITY', permission: 'ACTIVITY_ACCESS_ACTIVITY_MODULE' }],
  ['SADAyudantes', { module: 'ASSISTANT', permission: 'ASSISTANT_ACCESS_ASSISTANT_MODULE' }],
  ['SADProcesos', { module: 'PROCESS', permission: 'PROCESS_ACCESS_PROCESS_MODULE' }],
]);

export function hasClientAccess(access: unknown, clientId: string): boolean {
  if (typeof clientId !== 'string') return false;
  const policy = POLICIES.get(clientId.endsWith('-iis') ? clientId.slice(0, -4) : clientId);
  if (!policy || typeof access !== 'object' || access === null || Array.isArray(access)
    || !Object.prototype.hasOwnProperty.call(access, clientId)) return false;
  const entry: unknown = (access as Record<string, unknown>)[clientId];
  if (typeof entry !== 'object' || entry === null || Array.isArray(entry)) return false;
  const roles: unknown = (entry as { roles?: unknown }).roles;
  return Array.isArray(roles) && (
    roles.includes('ROLE_' + policy.module + '_ADMIN')
    || roles.includes('ROLE_' + policy.module + '_SUPERADMIN')
    || (policy.permission !== undefined && roles.includes('PERMISSION_' + policy.permission))
  );
}
