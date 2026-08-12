import PortalLayout from './PortalLayout.jsx';

export default function CitizenLayout() {
  return <PortalLayout title="Citizen Portal" subtitle="Report and track civic issues" navItems={[
    { to: '/citizen', label: 'Dashboard', end: true },
    { to: '/citizen/report', label: 'Report issue' },
    { to: '/citizen/complaints', label: 'My complaints' },
    { to: '/citizen/nearby', label: 'Nearby issues' },
    { to: '/citizen/profile', label: 'My profile' },
  ]} />;
}
