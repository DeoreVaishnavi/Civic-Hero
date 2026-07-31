export default function MetricCard({ label, value, hint, tone = 'sky', icon = '↗' }) {
  const toneClass = { sky: '', emerald: 'green', amber: 'amber', rose: 'red', violet: 'violet' }[tone] || '';
  return <article className={`stat-card ${toneClass}`}><span className="stat-icon">{icon}</span><small>{label}</small><strong>{value}</strong><p>{hint}</p></article>;
}
