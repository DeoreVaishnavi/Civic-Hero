import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import NotificationBell from '../components/common/NotificationBell.jsx';
import PortalAccountDrawer from '../components/common/PortalAccountDrawer.jsx';
import RoleBadge from '../components/common/RoleBadge.jsx';
import CivicIcon from '../components/ui/CivicIcon.jsx';
import CivicLogo from '../components/ui/CivicLogo.jsx';
import { useAuth } from '../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../routes/routePaths.js';

const iconFor = (label = '') => {
  const value = label.toLowerCase();
  if (value.includes('dashboard')) return 'dashboard';
  if (value.includes('report')) return 'plus';
  if (value.includes('complaint')) return 'complaints';
  if (value.includes('nearby') || value.includes('heat') || value.includes('map')) return 'map';
  if (value.includes('verify') || value.includes('verification') || value.includes('visual')) return 'verify';
  if (value.includes('dispute') || value.includes('appeal')) return 'dispute';
  if (value.includes('reward') || value.includes('leader')) return 'rewards';
  if (value.includes('chat') || value.includes('assistant')) return 'chat';
  if (value.includes('notification') || value.includes('broadcast')) return 'bell';
  if (value.includes('user') || value.includes('profile') || value.includes('staff')) return 'users';
  if (value.includes('security')) return 'security';
  if (value.includes('analytics') || value.includes('report')) return 'analytics';
  if (value.includes('health') || value.includes('quality') || value.includes('integration')) return 'health';
  if (value.includes('assignment') || value.includes('queue') || value.includes('overdue')) return 'assignment';
  if (value.includes('audit')) return 'audit';
  if (value.includes('governance') || value.includes('release') || value.includes('launch')) return 'governance';
  if (value.includes('master') || value.includes('department')) return 'department';
  if (value.includes('emergency') || value.includes('escalation')) return 'alert';
  if (value.includes('initiative') || value.includes('project')) return 'rewards';
  return 'info';
};

export default function PortalLayout({ title, subtitle, navItems, basePath }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);

  const initials = (user?.fullName || 'CivicHero User')
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();

  const handleLogout = async () => {
    setAccountOpen(false);
    await logout();
    navigate(ROUTE_PATHS.home, { replace: true });
  };

  return (
    <div className="civic-portal portal-shell">
      <button type="button" className={`portal-scrim ${open ? 'visible' : ''}`} aria-label="Close navigation" onClick={() => setOpen(false)} />
      <aside className={`portal-sidebar ${open ? 'open' : ''}`}>
        <div className="portal-brand-row">
          <CivicLogo to={basePath} />
          <button type="button" className="icon-button sidebar-close" onClick={() => setOpen(false)} aria-label="Close menu">
            <CivicIcon name="close" size={20} />
          </button>
        </div>

        <div className="portal-user-card">
          <div className="avatar">{initials}</div>
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
              title={item.label}
              onClick={() => setOpen(false)}
              className={({ isActive }) => `portal-nav-link ${isActive ? 'active' : ''}`}
            >
              <span className="nav-icon" aria-hidden="true"><CivicIcon name={iconFor(item.label)} size={18} /></span>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-support-card">
          <span aria-hidden="true"><CivicIcon name="help" size={18} /></span>
          <div><strong>Need help?</strong><small>Use the Civic Assistant or review your latest notifications.</small></div>
        </div>
        <button type="button" onClick={handleLogout} className="sidebar-logout">
          <CivicIcon name="logout" size={17} />
          <span>Sign out securely</span>
        </button>
      </aside>

      <div className="portal-workspace">
        <header className="portal-topbar">
          <div className="topbar-left">
            <button type="button" className="icon-button mobile-menu-button" onClick={() => setOpen(true)} aria-label="Open navigation">
              <CivicIcon name="menu" size={21} />
            </button>
            <div>
              <h1>{title}</h1>
              <p>{subtitle}</p>
            </div>
          </div>
          <div className="topbar-actions">
            <label className="portal-search">
              <CivicIcon name="search" size={17} />
              <input type="search" placeholder="Search complaints, IDs or users" aria-label="Search" />
            </label>
            <NotificationBell basePath={basePath} />
            <button
              type="button"
              className={`topbar-profile ${accountOpen ? 'active' : ''}`}
              title={`Open account menu for ${user?.fullName || 'user'}`}
              aria-haspopup="dialog"
              aria-expanded={accountOpen}
              aria-controls="portal-account-drawer"
              onClick={() => setAccountOpen((current) => !current)}
            >
              <span className="avatar small">{initials.slice(0, 1)}</span>
              <span className="topbar-profile-copy"><strong>{user?.fullName || 'User'}</strong><small>{user?.role || 'Citizen'}</small></span>
              <span className="topbar-profile-caret" aria-hidden="true"><CivicIcon name="chevron-down" size={16} /></span>
            </button>
          </div>
        </header>

        <PortalAccountDrawer
          open={accountOpen}
          onClose={() => setAccountOpen(false)}
          onLogout={handleLogout}
          user={user}
          profilePath={basePath === '/citizen' ? '/citizen/profile' : null}
        />

        <main className="portal-main"><Outlet /></main>
      </div>
    </div>
  );
}
