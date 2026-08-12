import PortalLayout from './PortalLayout.jsx';

export default function SupervisorLayout() {
  return <PortalLayout title="Supervisor Portal" subtitle="Department oversight, teams, and escalations" navItems={[{ to: '/supervisor', label: 'Team dashboard', end: true }, { to: '/supervisor/profile', label: 'My profile' }]} />;
}
