import { Link } from 'react-router-dom';
import CivicIcon from '../ui/CivicIcon.jsx';

export default function EmptyState({ title = 'Nothing to show yet', message = 'No records match the current view.', actionLabel, actionTo, icon = 'empty' }) {
  return (
    <div className="empty-state">
      <div className="empty-state-icon"><CivicIcon name={icon} size={27} /></div>
      <h3>{title}</h3>
      <p>{message}</p>
      {actionTo && <Link to={actionTo} className="button primary" style={{ marginTop: 20 }}>{actionLabel}</Link>}
    </div>
  );
}
