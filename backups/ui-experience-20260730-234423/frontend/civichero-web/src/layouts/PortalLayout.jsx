import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import NotificationBell from '../components/common/NotificationBell.jsx';
import RoleBadge from '../components/common/RoleBadge.jsx';
import CivicLogo from '../components/ui/CivicLogo.jsx';
import { useAuth } from '../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../routes/routePaths.js';

const iconFor = (label = '') => {
  const value = label.toLowerCase();
  if (value.includes('dashboard')) return '⌂';
  if (value.includes('report')) return '＋';
  if (value.includes('complaint')) return '▤';
  if (value.includes('nearby') || value.includes('heat')) return '⌖';
  if (value.includes('verify') || value.includes('verification')) return '✓';
  if (value.includes('dispute') || value.includes('appeal')) return '!';
  if (value.includes('reward') || value.includes('leader')) return '★';
  if (value.includes('chat') || value.includes('assistant')) return '◌';
  if (value.includes('notification') || value.includes('broadcast')) return '♢';
  if (value.includes('user') || value.includes('profile')) return '○';
  if (value.includes('security')) return '◇';
  if (value.includes('analytics') || value.includes('report')) return '↗';
  if (value.includes('health') || value.includes('quality')) return '◉';
  if (value.includes('assignment') || value.includes('queue')) return '≡';
  return '•';
};

export default function PortalLayout({ title, subtitle, navItems, basePath }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const handleLogout = async () => {
    await logout();
    navigate(ROUTE_PATHS.home, { replace: true });
  };

  return (
    <div className="civic-portal portal-shell">
      <button type="button" className={`portal-scrim ${open ? 'visible' : ''}`} aria-label="Close navigation" onClick={() => setOpen(false)} />
      <aside className={`portal-sidebar ${open ? 'open' : ''}`}>
        <div className="portal-brand-row">
          <CivicLogo to={basePath} />
          <button type="button" className="icon-button sidebar-close" onClick={() => setOpen(false)} aria-label="Close menu">×</button>
        </div>

        <div className="portal-user-card">
          <div className="avatar">{(user?.fullName || 'C').split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase()}</div>
          <div>
            <strong>{user?.fullName || 'CivicHero User'}</strong>
            <span>{user?.email || subtitle}</span>
          </div>
          <RoleBadge role={user?.role} />
        </div>

        <p className="nav-label">Workspace</p>
        <nav className="portal-nav" aria-label={`${title} navigation`}>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              onClick={() => setOpen(false)}
              className={({ isActive }) => `portal-nav-link ${isActive ? 'active' : ''}`}
            >
              <span className="nav-icon" aria-hidden="true">{iconFor(item.label)}</span>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-support-card">
          <span aria-hidden="true">?</span>
          <div><strong>Need help?</strong><small>Open the Civic Assistant or check your notifications.</small></div>
        </div>
        <button type="button" onClick={handleLogout} className="sidebar-logout">Sign out</button>
      </aside>

      <div className="portal-workspace">
        <header className="portal-topbar">
          <div className="topbar-left">
            <button type="button" className="icon-button mobile-menu-button" onClick={() => setOpen(true)} aria-label="Open navigation">☰</button>
            <div>
              <h1>{title}</h1>
              <p>{subtitle}</p>
            </div>
          </div>
          <div className="topbar-actions">
            <label className="portal-search">
              <span aria-hidden="true">⌕</span>
              <input type="search" placeholder="Search complaints, IDs or users" aria-label="Search" />
            </label>
            <NotificationBell basePath={basePath} />
            <button type="button" className="topbar-profile" title={user?.fullName}>
              <span className="avatar small">{(user?.fullName || 'C')[0].toUpperCase()}</span>
              <span className="topbar-profile-copy"><strong>{user?.fullName || 'User'}</strong><small>{user?.role || 'Citizen'}</small></span>
            </button>
          </div>
        </header>
        <main className="portal-main"><Outlet /></main>
      </div>
    </div>
  );
}
