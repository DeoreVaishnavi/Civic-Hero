export default function RoleBadge({ role }) {
  const tones = { Citizen: 'green', Officer: 'blue', Supervisor: 'amber', Admin: 'violet', SuperAdmin: 'red' };
  return <span className={`status-pill ${tones[role] || 'neutral'}`}><span className="status-dot" aria-hidden="true" />{role || 'Unknown'}</span>;
}
