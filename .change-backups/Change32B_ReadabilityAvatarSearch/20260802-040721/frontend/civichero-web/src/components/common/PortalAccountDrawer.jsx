import { useEffect } from 'react';
import { createPortal } from 'react-dom';
import { Link } from 'react-router-dom';
import RoleBadge from './RoleBadge.jsx';

const initialsFor = (fullName = '') => {
  const initials = fullName
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase();

  return initials || 'CH';
};

export default function PortalAccountDrawer({
  open,
  onClose,
  onLogout,
  user,
  profilePath,
}) {
  useEffect(() => {
    if (!open) return undefined;

    const previousOverflow = document.body.style.overflow;
    const handleKeyDown = (event) => {
      if (event.key === 'Escape') onClose();
    };

    document.body.style.overflow = 'hidden';
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [open, onClose]);

  if (!open) return null;

  const fullName = user?.fullName || 'CivicHero User';
  const email = user?.email || 'Account email unavailable';
  const role = user?.role || 'Citizen';

  return createPortal(
    <div className="account-drawer-layer" aria-live="polite">
      <button
        type="button"
        className="account-drawer-scrim"
        aria-label="Close account menu"
        onClick={onClose}
      />

      <aside
        id="portal-account-drawer"
        className="portal-account-drawer"
        role="dialog"
        aria-modal="true"
        aria-label="Account menu"
      >
        <div className="account-drawer-header">
          <div>
            <span className="account-drawer-eyebrow">CivicHero account</span>
            <h2>Account</h2>
          </div>
          <button
            type="button"
            className="account-drawer-close"
            aria-label="Close account menu"
            onClick={onClose}
          >
            ×
          </button>
        </div>

        <div className="account-drawer-profile-card">
          <div className="account-drawer-avatar" aria-hidden="true">
            {initialsFor(fullName)}
          </div>
          <div className="account-drawer-identity">
            <strong>{fullName}</strong>
            <span>{email}</span>
            <RoleBadge role={role} />
          </div>
        </div>

        <div className="account-drawer-actions">
          {profilePath && (
            <Link
              to={profilePath}
              className="account-drawer-action"
              onClick={onClose}
            >
              <span className="account-drawer-action-icon" aria-hidden="true">○</span>
              <span>
                <strong>View profile</strong>
                <small>Review and update your account details</small>
              </span>
              <span className="account-drawer-arrow" aria-hidden="true">›</span>
            </Link>
          )}

          <button
            type="button"
            className="account-drawer-action account-drawer-logout"
            onClick={onLogout}
          >
            <span className="account-drawer-action-icon" aria-hidden="true">↪</span>
            <span>
              <strong>Sign out</strong>
              <small>Close this CivicHero session securely</small>
            </span>
            <span className="account-drawer-arrow" aria-hidden="true">›</span>
          </button>
        </div>

        <p className="account-drawer-note">
          Your account menu closes when you select an action, click outside it, or press Escape.
        </p>
      </aside>
    </div>,
    document.body,
  );
}
