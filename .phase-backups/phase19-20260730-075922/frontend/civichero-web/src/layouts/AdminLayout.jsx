import PortalLayout from './PortalLayout.jsx';

export default function AdminLayout() {
  return <PortalLayout basePath="/admin" title="Administration" subtitle="Users, governance, broadcasts and system controls" navItems={[
    { to: '/admin', label: 'Admin dashboard', end: true },
    { to: '/admin/users', label: 'User management' },
    { to: '/admin/governance', label: 'Governance' },
    { to: '/admin/audit-logs', label: 'Audit logs' },
    { to: '/admin/system-health', label: 'System health' },
    { to: '/admin/security-center', label: 'Security centre' },
    { to: '/admin/quality-center', label: 'Quality centre' },
    { to: '/admin/release-center', label: 'Release centre' },
    { to: '/admin/launch-center', label: 'Launch & handover' },
    { to: '/admin/integration-center', label: 'Integration centre' },
    { to: '/admin/final-release', label: 'Final release' },
    { to: '/admin/appeals', label: 'Appeals' },
    { to: '/admin/verifications', label: 'Verification audit' },
    { to: '/admin/broadcast', label: 'Broadcast alerts' },
    { to: '/admin/ai-review', label: 'AI triage' },
    { to: '/admin/analytics', label: 'Analytics' },
    { to: '/admin/reports', label: 'Reports' },
    { to: '/admin/notifications', label: 'Notifications' },
    { to: '/admin/profile', label: 'My profile' },
    { to: '/admin/security', label: 'My security' },
  ]} />;
}
