import PortalLayout from './PortalLayout.jsx';

export default function SupervisorLayout() {
  return <PortalLayout title="Supervisor Portal" subtitle="Assignment, workload and SLA control" navItems={[
    { to: '/supervisor', label: 'Dashboard', end: true },
    { to: '/supervisor/assignments', label: 'Assignment queue' },
    { to: '/supervisor/overdue', label: 'SLA overdue' },
    { to: '/supervisor/verification', label: 'Verification queue' },
    { to: '/supervisor/disputes', label: 'Dispute review' },
    { to: '/supervisor/profile', label: 'My profile' },
  ]} />;
}
