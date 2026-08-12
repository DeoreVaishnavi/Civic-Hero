import PortalLayout from './PortalLayout.jsx';

export default function CitizenLayout() {
  return <PortalLayout basePath="/citizen" title="Citizen Portal" subtitle="Report, verify and earn for civic participation" navItems={[
    { to: '/citizen', label: 'Dashboard', end: true },
    { to: '/citizen/report', label: 'Report issue' },
    { to: '/citizen/complaints', label: 'My complaints' },
    { to: '/citizen/nearby', label: 'Nearby issues' },
    { to: '/citizen/verifications', label: 'Verify resolutions' },
    { to: '/citizen/disputes', label: 'Disputes & appeals' },
    { to: '/citizen/rewards', label: 'Rewards & rank' },
    { to: '/citizen/leaderboard', label: 'Leaderboard' },
    { to: '/citizen/chatbot', label: 'Civic assistant' },
    { to: '/citizen/notifications', label: 'Notifications' },
    { to: '/citizen/profile', label: 'My profile' },
  ]} />;
}
