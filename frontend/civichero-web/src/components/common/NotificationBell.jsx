import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import CivicIcon from '../ui/CivicIcon.jsx';
import { notificationApi } from '../../services/notificationApi.js';
import { startNotificationConnection } from '../../services/signalRService.js';
import { useNotificationStore } from '../../store/notificationStore.js';

export default function NotificationBell({ basePath }) {
  const { unreadCount, setUnreadCount, connectionState } = useNotificationStore();
  useEffect(() => {
    notificationApi.unread().then((result) => setUnreadCount(result.count ?? 0)).catch(() => undefined);
    startNotificationConnection();
  }, [setUnreadCount]);

  const connectionLabel = connectionState === 'connected'
    ? 'Notifications are connected'
    : 'Notifications are connecting';

  return (
    <Link
      to={`${basePath}/notifications`}
      className="icon-button"
      style={{ position: 'relative' }}
      aria-label={`${unreadCount} unread notifications. ${connectionLabel}.`}
      title={connectionLabel}
    >
      <CivicIcon name="bell" size={19} />
      {unreadCount > 0 && (
        <span className="notification-count-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
      )}
    </Link>
  );
}
