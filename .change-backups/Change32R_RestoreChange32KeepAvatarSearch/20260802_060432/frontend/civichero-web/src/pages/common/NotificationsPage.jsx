import { useEffect, useMemo, useState } from 'react';
import { notificationApi } from '../../services/notificationApi.js';
import { useNotificationStore } from '../../store/notificationStore.js';
import CivicIcon from '../../components/ui/CivicIcon.jsx';

const filters = ['All', 'Complaints', 'Disputes', 'Rewards', 'System'];
const icons = { complaint: 'complaints', dispute: 'dispute', reward: 'rewards', security: 'security', system: 'info', assignment: 'assignment', verification: 'verify' };

export default function NotificationsPage() {
  const { items, unreadCount, setNotifications, markRead, markAllRead, remove, connectionState } = useNotificationStore();
  const [preferences, setPreferences] = useState(null);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [activeFilter, setActiveFilter] = useState('All');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const load = async () => {
    setError('');
    try {
      const [notifications, prefs] = await Promise.all([notificationApi.list({ pageSize: 100, unreadOnly }), notificationApi.preferences()]);
      setNotifications(notifications);
      setPreferences(prefs);
    } catch (reason) { setError(reason.message); }
  };

  useEffect(() => { load(); }, [unreadOnly]);

  const visibleItems = useMemo(() => items.filter((item) => {
    if (activeFilter === 'All') return true;
    const type = String(item.type || '').toLowerCase();
    if (activeFilter === 'Complaints') return type.includes('complaint') || type.includes('assignment') || type.includes('verification');
    if (activeFilter === 'Disputes') return type.includes('dispute') || type.includes('appeal');
    if (activeFilter === 'Rewards') return type.includes('reward') || type.includes('badge');
    return type.includes('system') || type.includes('security') || type.includes('broadcast');
  }), [items, activeFilter]);

  const read = async (item) => {
    if (!item.isRead) { await notificationApi.markRead(item.id); markRead(item.id); }
    if (item.actionUrl) window.location.assign(item.actionUrl);
  };
  const readAll = async () => { await notificationApi.markAllRead(); markAllRead(); setMessage('All notifications marked as read.'); };
  const deleteItem = async (id) => { await notificationApi.remove(id); remove(id); };
  const togglePreference = async (key) => {
    const next = { ...preferences, [key]: !preferences[key] };
    setPreferences(await notificationApi.updatePreferences(next));
    setMessage('Notification preferences saved.');
  };

  return (
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Real-time notification centre</p><h2>Notifications</h2><p>Stay updated with complaint, dispute, reward and system alerts.</p></div><div className="page-actions"><button type="button" onClick={() => setUnreadOnly((value) => !value)} className="button outline">{unreadOnly ? 'Show all' : `Unread only (${unreadCount})`}</button><button type="button" onClick={readAll} className="button primary">Mark all as read</button></div></div>
      {error && <div className="alert error">{error}</div>}{message && <div className="alert success">{message}</div>}

      <div className="mt-5 flex gap-2 overflow-x-auto rounded-2xl border border-slate-200 bg-white p-2 shadow-sm">{filters.map((filter) => <button key={filter} type="button" onClick={() => setActiveFilter(filter)} className={`shrink-0 rounded-xl px-5 py-3 text-sm font-black transition ${activeFilter === filter ? 'notification-filter-active' : 'notification-filter-inactive'}`}>{filter}{filter === 'Complaints' && unreadCount > 0 ? ` ${unreadCount}` : ''}</button>)}</div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[1.35fr_.65fr]">
        <div className="overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-200 px-6 py-5"><div className="flex items-center justify-between"><div><p className="text-xs font-black uppercase tracking-[.18em] text-blue-600">Notification list</p><h3 className="mt-1 text-xl font-black text-slate-900">{visibleItems.length} alerts</h3></div><span className={`inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-xs font-black ${connectionState === 'connected' ? 'cv-status-green' : 'cv-status-amber'}`}><span className="notification-connection-dot" aria-hidden="true" />SignalR {connectionState}</span></div></div>
          <div className="divide-y divide-slate-100">{visibleItems.map((item) => {
            const type = String(item.type || 'system').toLowerCase();
            const icon = Object.entries(icons).find(([key]) => type.includes(key))?.[1] || 'info';
            return <article key={item.id} className={`group flex gap-4 p-5 transition hover:bg-blue-50/40 ${item.isRead ? 'bg-white' : 'bg-blue-50/60'}`}>
              <button type="button" onClick={() => read(item)} className="notification-icon-button grid h-14 w-14 shrink-0 place-items-center rounded-2xl transition group-hover:scale-105"><CivicIcon name={icon} size={24} /></button>
              <button type="button" onClick={() => read(item)} className="min-w-0 flex-1 text-left"><div className="flex flex-wrap items-center gap-2"><span className="rounded-full bg-slate-100 px-2.5 py-1 text-[10px] font-black uppercase tracking-wider text-slate-600">{item.type}</span>{!item.isRead && <span className="rounded-full bg-rose-100 px-2.5 py-1 text-[10px] font-black text-rose-700">NEW</span>}</div><h3 className="mt-3 text-base font-black text-slate-900">{item.title}</h3><p className="mt-2 text-sm leading-6 text-slate-600">{item.message}</p><p className="mt-3 text-xs font-bold text-slate-400">{new Date(item.createdAt).toLocaleString()}</p></button>
              <button type="button" onClick={() => deleteItem(item.id)} className="self-start rounded-xl border border-slate-200 px-3 py-2 text-xs font-bold text-slate-500 transition hover:border-rose-200 hover:bg-rose-50 hover:text-rose-700">Remove</button>
            </article>;
          })}{visibleItems.length === 0 && <div className="p-14 text-center"><div className="mx-auto grid h-16 w-16 place-items-center rounded-3xl bg-slate-100 text-slate-700"><CivicIcon name="bell" size={28} /></div><h3 className="mt-5 text-lg font-black text-slate-900">No notifications found</h3><p className="mt-2 text-sm text-slate-500">There are no alerts matching this filter.</p></div>}</div>
        </div>

        <aside className="rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-sm"><h3 className="text-xl font-black text-slate-900">Preferences</h3><p className="mt-2 text-sm leading-6 text-slate-500">Choose the notification channels and categories you want to receive.</p><div className="mt-5 space-y-3">{preferences && Object.entries({ inAppEnabled:'In-app notifications', emailEnabled:'Email notifications', smsEnabled:'SMS critical alerts', complaintUpdates:'Complaint updates', assignmentUpdates:'Assignment and SLA', verificationUpdates:'Verification reminders', disputeUpdates:'Dispute decisions', rewardUpdates:'Rewards and badges', securityAlerts:'Security alerts' }).map(([key,label]) => <label key={key} className="flex cursor-pointer items-center justify-between gap-3 rounded-xl border border-slate-200 p-3 transition hover:border-blue-200 hover:bg-blue-50/40"><span className="text-sm font-bold text-slate-700">{label}</span><input type="checkbox" checked={Boolean(preferences[key])} onChange={() => togglePreference(key)} className="h-5 w-5 accent-blue-600" /></label>)}</div></aside>
      </div>
    </section>
  );
}
