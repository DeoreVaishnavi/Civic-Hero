import PortalLayout from './PortalLayout.jsx';

export default function OfficerLayout() {
  return <PortalLayout title="Officer Portal" subtitle="Work queue scoped to your department and ward" navItems={[{ to: '/officer', label: 'Work dashboard', end: true }, { to: '/officer/profile', label: 'My profile' }]} />;
}
