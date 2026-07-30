import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { notificationApi } from '../../services/notificationApi.js';
import { startNotificationConnection } from '../../services/signalRService.js';
import { useNotificationStore } from '../../store/notificationStore.js';

export default function NotificationBell({ basePath }) {
  const { unreadCount, setUnreadCount, connectionState } = useNotificationStore();

  useEffect(() => {
    notificationApi.unread().then((result) => setUnreadCount(result.count ?? 0)).catch(() => undefined);
    startNotificationConnection();
  }, [setUnreadCount]);

  return (
    <Link to={`${basePath}/notifications`} className="relative inline-flex h-11 items-center gap-2 rounded-xl border border-white/10 bg-white/5 px-4 text-sm font-bold text-slate-200 hover:bg-white/10" aria-label={`${unreadCount} unread notifications`}>
      <span aria-hidden="true">🔔</span>
      <span className="hidden sm:inline">Alerts</span>
      {unreadCount > 0 && <span className="absolute -right-2 -top-2 min-w-6 rounded-full bg-rose-500 px-1.5 py-1 text-center text-xs text-white">{unreadCount > 99 ? '99+' : unreadCount}</span>}
      <span className={`h-2 w-2 rounded-full ${connectionState === 'connected' ? 'bg-emerald-400' : 'bg-slate-500'}`} />
    </Link>
  );
}
