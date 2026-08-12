export const ROLES = Object.freeze({
  citizen: 'Citizen',
  officer: 'Officer',
  supervisor: 'Supervisor',
  admin: 'Admin',
  superAdmin: 'SuperAdmin',
});

export function dashboardForRole(role) {
  switch ((role || '').toLowerCase()) {
    case 'officer': return '/officer';
    case 'supervisor': return '/supervisor';
    case 'admin':
    case 'superadmin': return '/admin';
    default: return '/citizen';
  }
}

export function profileForRole(role) {
  return `${dashboardForRole(role)}/profile`;
}
