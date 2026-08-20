import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { notificationApi } from '../../services/notificationApi.js';
import { startNotificationConnection } from '../../services/signalRService.js';
import { useNotificationStore } from '../../store/notificationStore.js';

export default function NotificationBell({ basePath }) {
  const { unreadCount, setUnreadCount, connectionState } = useNotificationStore();
  useEffect(() => { notificationApi.unread().then((result) => setUnreadCount(result.count ?? 0)).catch(() => undefined); startNotificationConnection(); }, [setUnreadCount]);
  return <Link to={`${basePath}/notifications`} className="icon-button" style={{ position: 'relative' }} aria-label={`${unreadCount} unread notifications`} title={connectionState === 'connected' ? 'Notifications connected' : 'Notifications connecting'}><span aria-hidden="true">♢</span>{unreadCount > 0 && <span style={{ position: 'absolute', right: -6, top: -6, minWidth: 20, height: 20, padding: '0 4px', display: 'grid', placeItems: 'center', borderRadius: 99, background: 'var(--civic-red)', color: 'white', fontSize: 8, fontWeight: 900 }}>{unreadCount > 99 ? '99+' : unreadCount}</span>}</Link>;
}
