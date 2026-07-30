const styles = {
  OnTrack: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200',
  AtRisk: 'border-amber-400/30 bg-amber-400/10 text-amber-200',
  Breached: 'border-rose-400/30 bg-rose-400/10 text-rose-200',
  Unassigned: 'border-slate-400/30 bg-slate-400/10 text-slate-200',
};

function duration(minutes) {
  const absolute = Math.abs(minutes || 0);
  if (absolute < 60) return `${absolute}m`;
  if (absolute < 1440) return `${Math.floor(absolute / 60)}h ${absolute % 60}m`;
  return `${Math.floor(absolute / 1440)}d ${Math.floor((absolute % 1440) / 60)}h`;
}

export default function SlaBadge({ state, remainingMinutes }) {
  const label = state === 'Breached' ? `${duration(remainingMinutes)} overdue` : state === 'Unassigned' ? 'Awaiting assignment' : `${duration(remainingMinutes)} left`;
  return <span className={`inline-flex rounded-full border px-3 py-1 text-xs font-black ${styles[state] || styles.OnTrack}`}>{label}</span>;
}
