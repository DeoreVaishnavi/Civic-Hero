import PortalLayout from './PortalLayout.jsx';

export default function OfficerLayout() {
  return <PortalLayout basePath="/officer" title="Officer Portal" subtitle="Assigned field work, SLA and resolution evidence" navItems={[
    { to: '/officer', label: 'Dashboard', end: true },
    { to: '/officer/assignments', label: 'Work queue' },
    { to: '/officer/notifications', label: 'Notifications' },
    { to: '/officer/profile', label: 'My profile' },
  ]} />;
}
