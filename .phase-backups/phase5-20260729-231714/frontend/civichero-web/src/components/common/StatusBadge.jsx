const styles = {
  Created: 'border-slate-400/30 bg-slate-400/10 text-slate-200',
  AiTriage: 'border-cyan-400/30 bg-cyan-400/10 text-cyan-200',
  Assigned: 'border-sky-400/30 bg-sky-400/10 text-sky-200',
  InProgress: 'border-amber-400/30 bg-amber-400/10 text-amber-200',
  Resolved: 'border-violet-400/30 bg-violet-400/10 text-violet-200',
  VerificationPending: 'border-fuchsia-400/30 bg-fuchsia-400/10 text-fuchsia-200',
  Closed: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200',
  ClosedAuto: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200',
  Escalated: 'border-rose-400/30 bg-rose-400/10 text-rose-200',
  Disputed: 'border-red-500/30 bg-red-500/10 text-red-200',
  Withdrawn: 'border-slate-600 bg-slate-800 text-slate-400',
};

function readable(value = '') {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export default function StatusBadge({ status }) {
  return <span className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-bold ${styles[status] || styles.Created}`}>{readable(status)}</span>;
}
