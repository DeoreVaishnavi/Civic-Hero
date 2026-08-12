import PortalLayout from './PortalLayout.jsx';

export default function AdminLayout() {
  return <PortalLayout basePath="/admin" title="Administration" subtitle="Users, governance, broadcasts and system controls" navItems={[
    { to: '/admin', label: 'Admin dashboard', end: true },
    { to: '/admin/users', label: 'User management' },
    { to: '/admin/appeals', label: 'Appeals' },
    { to: '/admin/verifications', label: 'Verification audit' },
    { to: '/admin/broadcast', label: 'Broadcast alerts' },
    { to: '/admin/notifications', label: 'Notifications' },
    { to: '/admin/profile', label: 'My profile' },
  ]} />;
}
