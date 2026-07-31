import { Link } from 'react-router-dom';
import CivicLogo from '../ui/CivicLogo.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { dashboardForRole } from '../../utils/roleRouting.js';

export default function Header() {
  const { isAuthenticated, user } = useAuth();
  return (
    <header className="site-header">
      <div className="site-header-inner">
        <CivicLogo />
        <nav className="primary-nav" aria-label="Public navigation">
          <Link to={ROUTE_PATHS.home}>Home</Link>
          <Link to={ROUTE_PATHS.anonymousReport}>Report complaint</Link>
          <Link to={ROUTE_PATHS.anonymousTrack}>Track complaint</Link>
          <a href="#how-it-works">How it works</a>
          <a href="#city-impact">City impact</a>
        </nav>
        <div className="site-header-actions">
          {isAuthenticated ? (
            <Link to={dashboardForRole(user?.role)} className="button primary small">Open dashboard</Link>
          ) : (
            <>
              <Link to={ROUTE_PATHS.login} className="button ghost small">Login</Link>
              <Link to={ROUTE_PATHS.register} className="button primary small">Create account</Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
