import { Link } from 'react-router-dom';

export default function CivicLogo({ to = '/', compact = false, light = false }) {
  return (
    <Link to={to} className={`civic-logo ${compact ? 'compact' : ''} ${light ? 'light' : ''}`} aria-label="CivicHero home">
      <span className="civic-logo-mark" aria-hidden="true">
        <span className="roof" />
        <span className="building">CH</span>
      </span>
      {!compact && (
        <span className="civic-logo-copy">
          <strong>Civic<span>Hero</span></strong>
          <small>REPORT • VERIFY • IMPROVE</small>
        </span>
      )}
    </Link>
  );
}
