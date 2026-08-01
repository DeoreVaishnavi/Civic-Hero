import { useCallback, useEffect, useState } from 'react';
import { securityApi } from '../../services/securityApi.js';

const fmt = (value) => value ? new Date(value).toLocaleString() : '—';

export default function SecurityCenter() {
  const [overview, setOverview] = useState(null);
  const [events, setEvents] = useState([]);
  const [locked, setLocked] = useState([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busyId, setBusyId] = useState(null);

  const load = useCallback(async () => {
    setError('');
    try {
      const [nextOverview, nextEvents, nextLocked] = await Promise.all([
        securityApi.getOverview(), securityApi.getEvents(75), securityApi.getLockedAccounts(),
      ]);
      setOverview(nextOverview); setEvents(nextEvents); setLocked(nextLocked);
    } catch (reason) { setError(reason.message || 'Unable to load the security centre.'); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const run = async (id, action) => {
    setBusyId(`${action}-${id}`); setError(''); setMessage('');
    try {
      const result = action === 'unlock' ? await securityApi.unlockAccount(id) : await securityApi.revokeUserSessions(id);
      setMessage(`${result.email}: ${result.action}.`); await load();
    } catch (reason) { setError(reason.message || 'Security action failed.'); }
    finally { setBusyId(null); }
  };

  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-400">Security operations</p><h2 className="mt-2 text-3xl font-black text-white">Security centre</h2><p className="mt-2 text-slate-400">Monitor authentication risk, lockouts, rejected traffic and active sessions.</p></div>
      <button onClick={load} className="rounded-xl border border-white/15 px-4 py-2 text-sm font-bold text-white hover:bg-white/10">Refresh</button>
    </div>
    {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-200">{error}</div>}
    {message && <div className="rounded-xl border border-emerald-400/30 bg-emerald-400/10 p-4 text-emerald-200">{message}</div>}

    {overview && <>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Metric label="Active sessions" value={overview.activeRefreshSessions} hint={`${overview.activeUsers} active users`} />
        <Metric label="Locked accounts" value={overview.lockedAccounts} hint={`${overview.usersWithFailedAttempts} with failed attempts`} />
        <Metric label="Auth failures (24h)" value={overview.authenticationFailuresLast24Hours} hint={`${overview.distinctFailureAddressesLast24Hours} source addresses`} />
        <Metric label="Rate-limit rejections" value={overview.rateLimits.rejectedRequests} hint={`${overview.rateLimits.allowedRequests} allowed since restart`} />
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        <Metric label="Auth throttles" value={overview.rateLimits.authenticationRejections} hint="Strict login/register policy" />
        <Metric label="Upload throttles" value={overview.rateLimits.uploadRejections} hint="Complaint and evidence uploads" />
        <Metric label="Admin throttles" value={overview.rateLimits.administrationRejections} hint="Governance and security APIs" />
      </div>
    </>}

    <div className="rounded-2xl border border-white/10 bg-white/5 p-5">
      <h3 className="text-xl font-bold text-white">Accounts requiring attention</h3>
      <div className="mt-4 overflow-x-auto">
        <table className="min-w-full text-left text-sm"><thead className="text-xs uppercase tracking-wider text-slate-500"><tr><th className="p-3">User</th><th className="p-3">Role</th><th className="p-3">Failed attempts</th><th className="p-3">Locked until</th><th className="p-3">Actions</th></tr></thead>
          <tbody className="divide-y divide-white/10">{locked.length === 0 ? <tr><td colSpan="5" className="p-5 text-center text-slate-400">No accounts currently require attention.</td></tr> : locked.map((user) => <tr key={user.id}>
            <td className="p-3"><p className="font-semibold text-white">{user.fullName}</p><p className="text-xs text-slate-500">{user.email}</p></td><td className="p-3 text-slate-300">{user.role}</td><td className="p-3 text-slate-300">{user.failedLoginAttempts}</td><td className="p-3 text-slate-300">{fmt(user.lockoutEndUtc)}</td><td className="p-3"><div className="flex gap-2"><button disabled={busyId === `unlock-${user.id}`} onClick={() => run(user.id, 'unlock')} className="rounded-lg bg-emerald-500/20 px-3 py-2 text-xs font-bold text-emerald-200">Unlock</button><button disabled={busyId === `revoke-${user.id}`} onClick={() => run(user.id, 'revoke')} className="rounded-lg bg-rose-500/20 px-3 py-2 text-xs font-bold text-rose-200">Revoke sessions</button></div></td>
          </tr>)}</tbody></table>
      </div>
    </div>

    <div className="rounded-2xl border border-white/10 bg-white/5 p-5">
      <h3 className="text-xl font-bold text-white">Recent security events</h3>
      <div className="mt-4 space-y-3">{events.length === 0 ? <p className="text-slate-400">No security events recorded.</p> : events.map((event) => <div key={event.id} className="grid gap-3 rounded-xl border border-white/10 bg-slate-950/40 p-4 md:grid-cols-[1fr_auto]">
        <div><div className="flex flex-wrap items-center gap-2"><span className={`rounded-full px-2 py-1 text-xs font-bold ${event.success ? 'bg-emerald-400/10 text-emerald-200' : 'bg-rose-400/10 text-rose-200'}`}>{event.success ? 'Success' : 'Failed'}</span><span className="font-semibold text-white">{event.action}</span><span className="text-sm text-slate-500">{event.entityName}</span></div><p className="mt-2 text-sm text-slate-400">{event.userEmail || 'Anonymous'} · {event.ipAddress || 'Unknown IP'} · HTTP {event.httpStatusCode}</p>{event.errorMessage && <p className="mt-1 text-sm text-rose-300">{event.errorMessage}</p>}<p className="mt-1 break-all text-xs text-slate-600">Correlation: {event.correlationId || '—'}</p></div><time className="text-xs text-slate-500">{fmt(event.createdAtUtc)}</time>
      </div>)}</div>
    </div>
  </section>;
}

function Metric({ label, value, hint }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 text-3xl font-black text-white">{value ?? '—'}</p><p className="mt-2 text-xs text-slate-400">{hint}</p></div>; }
