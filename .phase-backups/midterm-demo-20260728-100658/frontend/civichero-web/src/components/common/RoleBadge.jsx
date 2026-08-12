export default function RoleBadge({ role }) {
  const styles = {
    Citizen: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200',
    Officer: 'border-sky-400/30 bg-sky-400/10 text-sky-200',
    Supervisor: 'border-amber-400/30 bg-amber-400/10 text-amber-200',
    Admin: 'border-violet-400/30 bg-violet-400/10 text-violet-200',
    SuperAdmin: 'border-rose-400/30 bg-rose-400/10 text-rose-200',
  };
  return <span className={`rounded-full border px-2.5 py-1 text-xs font-bold ${styles[role] || styles.Citizen}`}>{role || 'Unknown'}</span>;
}
