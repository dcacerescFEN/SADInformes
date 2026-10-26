const POLICIES = new Map<string, { module: string; permission?: string }>([
  ['NewSAD', { module: 'SAD', permission: 'SAD_ACCESS' }],
  ['SADAlumnos', { module: 'STUDENT', permission: 'STUDENT_ACCESS_STUDENT_MODULE' }],
  ['SADInformes', { module: 'REPORT', permission: 'REPORT_ACCESS' }],
  ['SADAdministracion', { module: 'ADMINISTRATION', permission: 'ADMINISTRATION_ACCESS' }],
  ['SADProfesores', { module: 'TEACHER' }],
  ['SADCursos', { module: 'COURSE' }],
  ['SADExamenes', { module: 'EXAM' }],
  ['SADActividades', { module: 'ACTIVITY' }],
  ['SADAyudantes', { module: 'ASSISTANT' }],
  ['SADProcesos', { module: 'PROCESS' }],
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
