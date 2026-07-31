import { useEffect, useState } from 'react';
import { notificationApi } from '../../services/notificationApi.js';
import { useNotificationStore } from '../../store/notificationStore.js';

export default function NotificationsPage() {
  const { items, unreadCount, setNotifications, markRead, markAllRead, remove, connectionState } = useNotificationStore();
  const [preferences, setPreferences] = useState(null);
  const [unreadOnly, setUnreadOnly] = useState(false);
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

  const read = async (item) => {
    if (!item.isRead) { await notificationApi.markRead(item.id); markRead(item.id); }
    if (item.actionUrl) window.location.assign(item.actionUrl);
  };

  const readAll = async () => { await notificationApi.markAllRead(); markAllRead(); setMessage('All notifications marked as read.'); };
  const deleteItem = async (id) => { await notificationApi.remove(id); remove(id); };
  const togglePreference = async (key) => {
    const next = { ...preferences, [key]: !preferences[key] };
    setPreferences(await notificationApi.updatePreferences(next));
    setMessage('Preferences saved.');
  };

  return <section className="p-6 lg:p-10">
    <div className="flex flex-wrap items-center justify-between gap-4">
      <div><p className="text-sm font-bold uppercase tracking-wider text-sky-300">Real-time notification centre</p><h2 className="mt-2 text-3xl font-black text-white">Notifications</h2><p className="mt-2 text-slate-400">SignalR: <span className={connectionState === 'connected' ? 'text-emerald-300' : 'text-amber-300'}>{connectionState}</span> · {unreadCount} unread</p></div>
      <div className="flex gap-2"><button onClick={() => setUnreadOnly((value) => !value)} className="rounded-xl border border-white/10 px-4 py-2 font-bold hover:bg-white/10">{unreadOnly ? 'Show all' : 'Unread only'}</button><button onClick={readAll} className="rounded-xl bg-sky-500 px-4 py-2 font-bold text-white">Read all</button></div>
    </div>
    {error && <Alert tone="error">{error}</Alert>}{message && <Alert>{message}</Alert>}
    <div className="mt-8 grid gap-6 xl:grid-cols-[1.4fr_.8fr]">
      <div className="space-y-3">{items.map((item) => <article key={item.id} className={`rounded-2xl border p-5 ${item.isRead ? 'border-white/10 bg-white/5' : 'border-sky-400/30 bg-sky-400/10'}`}>
        <div className="flex items-start justify-between gap-4"><button onClick={() => read(item)} className="min-w-0 flex-1 text-left"><div className="flex flex-wrap items-center gap-2"><span className="rounded-full bg-white/10 px-2 py-1 text-xs font-bold text-sky-200">{item.type}</span>{!item.isRead && <span className="text-xs font-bold text-rose-300">NEW</span>}</div><h3 className="mt-3 text-lg font-black text-white">{item.title}</h3><p className="mt-2 text-sm leading-6 text-slate-300">{item.message}</p><p className="mt-3 text-xs text-slate-500">{new Date(item.createdAt).toLocaleString()}</p></button><button onClick={() => deleteItem(item.id)} className="rounded-lg border border-white/10 px-3 py-2 text-xs text-slate-400 hover:bg-white/10">Remove</button></div>
      </article>)}{items.length === 0 && <div className="rounded-2xl border border-dashed border-white/15 p-10 text-center text-slate-400">No notifications match this view.</div>}</div>
      <aside className="rounded-2xl border border-white/10 bg-white/5 p-6"><h3 className="text-xl font-black text-white">Preferences</h3><p className="mt-2 text-sm text-slate-400">Choose which notification categories appear in your in-app inbox.</p><div className="mt-5 space-y-3">{preferences && Object.entries({ inAppEnabled:'In-app notifications', emailEnabled:'Email notifications', smsEnabled:'SMS critical alerts', complaintUpdates:'Complaint updates', assignmentUpdates:'Assignment and SLA', verificationUpdates:'Verification reminders', disputeUpdates:'Dispute decisions', rewardUpdates:'Rewards and badges', securityAlerts:'Security alerts' }).map(([key,label]) => <label key={key} className="flex cursor-pointer items-center justify-between rounded-xl border border-white/10 p-3"><span className="text-sm font-semibold text-slate-300">{label}</span><input type="checkbox" checked={Boolean(preferences[key])} onChange={() => togglePreference(key)} className="h-5 w-5" /></label>)}</div></aside>
    </div>
  </section>;
}

function Alert({ children, tone = 'success' }) { return <div className={`mt-5 rounded-xl border p-4 ${tone === 'error' ? 'border-rose-400/30 bg-rose-400/10 text-rose-100' : 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100'}`}>{children}</div>; }
