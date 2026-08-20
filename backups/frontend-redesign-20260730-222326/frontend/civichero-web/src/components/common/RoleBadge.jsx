export default function RoleBadge({ role }) {
  const tones = { Citizen: 'green', Officer: '', Supervisor: 'amber', Admin: 'violet', SuperAdmin: 'red' };
  return <span className={`status-pill ${tones[role] || ''}`}>{role || 'Unknown'}</span>;
}
