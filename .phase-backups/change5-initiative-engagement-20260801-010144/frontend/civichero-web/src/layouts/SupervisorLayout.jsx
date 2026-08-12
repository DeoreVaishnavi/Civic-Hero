import PortalLayout from './PortalLayout.jsx';
export default function SupervisorLayout() { return <PortalLayout basePath="/supervisor" title="Supervisor Portal" subtitle="Assignment, workload, SLA and evidence control" navItems={[
  { to: '/supervisor', label: 'Dashboard', end: true }, { to: '/supervisor/assignments', label: 'Assignment queue' }, { to: '/supervisor/overdue', label: 'SLA overdue' },
  { to: '/supervisor/emergency-reviews', label: 'Emergency review' }, { to: '/supervisor/comment-moderation', label: 'Comment moderation' }, { to: '/supervisor/visual-verification', label: 'AI visual review' }, { to: '/supervisor/verifications', label: 'Verification queue' },
  { to: '/supervisor/disputes', label: 'Dispute review' }, { to: '/supervisor/analytics', label: 'Team analytics' }, { to: '/supervisor/notifications', label: 'Notifications' }, { to: '/supervisor/profile', label: 'My profile' }, { to: '/supervisor/security', label: 'Security & sessions' },
]} />; }
