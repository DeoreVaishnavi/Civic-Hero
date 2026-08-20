const tones = {
  OnTrack: 'on-track',
  AtRisk: 'at-risk',
  Breached: 'breached',
  Unassigned: 'unassigned',
};

function duration(minutes) {
  const absolute = Math.abs(minutes || 0);
  if (absolute < 60) return `${absolute}m`;
  if (absolute < 1440) return `${Math.floor(absolute / 60)}h ${absolute % 60}m`;
  return `${Math.floor(absolute / 1440)}d ${Math.floor((absolute % 1440) / 60)}h`;
}

export default function SlaBadge({ state, remainingMinutes }) {
  const label = state === 'Breached'
    ? `${duration(remainingMinutes)} overdue`
    : state === 'Unassigned'
      ? 'Awaiting assignment'
      : `${duration(remainingMinutes)} left`;
  return <span className={`sla-badge ${tones[state] || tones.OnTrack}`}><span className="status-dot" aria-hidden="true" />{label}</span>;
}
