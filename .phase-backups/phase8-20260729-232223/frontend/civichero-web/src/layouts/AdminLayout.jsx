import PortalLayout from './PortalLayout.jsx';

export default function AdminLayout() {
  return <PortalLayout title="Administration" subtitle="Users, roles, access scopes, and system governance" navItems={[{ to: '/admin', label: 'Admin dashboard', end: true }, { to: '/admin/users', label: 'User management' }, { to: '/admin/appeals', label: 'Appeals' }, { to: '/admin/profile', label: 'My profile' }]} />;
}
