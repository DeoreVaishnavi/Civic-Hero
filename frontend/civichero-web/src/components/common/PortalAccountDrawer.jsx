import { useEffect } from 'react';
import { createPortal } from 'react-dom';
import { Link } from 'react-router-dom';
import CivicIcon from '../ui/CivicIcon.jsx';
import RoleBadge from './RoleBadge.jsx';
import UserAvatar from './UserAvatar.jsx';

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
      <button type="button" className="account-drawer-scrim" aria-label="Close account menu" onClick={onClose} />

      <aside id="portal-account-drawer" className="portal-account-drawer" role="dialog" aria-modal="true" aria-label="Account menu">
        <div className="account-drawer-header">
          <div>
            <span className="account-drawer-eyebrow">CivicHero account</span>
            <h2>Account</h2>
          </div>
          <button type="button" className="account-drawer-close" aria-label="Close account menu" onClick={onClose}>
            <CivicIcon name="close" size={19} />
          </button>
        </div>

        <div className="account-drawer-profile-card">
          <UserAvatar user={user} className="account-drawer-avatar" />
          <div className="account-drawer-identity">
            <strong>{fullName}</strong>
            <span>{email}</span>
            <RoleBadge role={role} />
          </div>
        </div>

        <div className="account-drawer-actions">
          {profilePath && (
            <Link to={profilePath} className="account-drawer-action" onClick={onClose}>
              <span className="account-drawer-action-icon" aria-hidden="true"><CivicIcon name="user" size={19} /></span>
              <span><strong>View profile</strong><small>Review and update your account details</small></span>
              <span className="account-drawer-arrow" aria-hidden="true"><CivicIcon name="arrow" size={17} /></span>
            </Link>
          )}

          <button type="button" className="account-drawer-action account-drawer-logout" onClick={onLogout}>
            <span className="account-drawer-action-icon" aria-hidden="true"><CivicIcon name="logout" size={19} /></span>
            <span><strong>Sign out</strong><small>Close this CivicHero session securely</small></span>
            <span className="account-drawer-arrow" aria-hidden="true"><CivicIcon name="arrow" size={17} /></span>
          </button>
        </div>

        <p className="account-drawer-note">Your account menu closes when you select an action, click outside it, or press Escape.</p>
      </aside>
    </div>,
    document.body,
  );
}
