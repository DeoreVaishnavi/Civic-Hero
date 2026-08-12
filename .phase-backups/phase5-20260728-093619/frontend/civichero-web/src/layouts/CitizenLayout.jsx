import PortalLayout from './PortalLayout.jsx';

export default function CitizenLayout() {
  return <PortalLayout title="Citizen Portal" subtitle="Report and track civic issues" navItems={[{ to: '/citizen', label: 'Dashboard', end: true }, { to: '/citizen/profile', label: 'My profile' }]} />;
}
