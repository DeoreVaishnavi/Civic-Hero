import CivicIcon from '../ui/CivicIcon.jsx';

export default function MetricCard({ label, value, hint, helper, tone = 'sky', icon = 'analytics' }) {
  const toneClass = { sky: '', emerald: 'green', amber: 'amber', rose: 'red', violet: 'violet' }[tone] || '';
  return (
    <article className={`stat-card ${toneClass}`}>
      <span className="stat-icon"><CivicIcon name={icon} size={20} /></span>
      <small>{label}</small>
      <strong>{value}</strong>
      <p>{hint || helper}</p>
    </article>
  );
}
