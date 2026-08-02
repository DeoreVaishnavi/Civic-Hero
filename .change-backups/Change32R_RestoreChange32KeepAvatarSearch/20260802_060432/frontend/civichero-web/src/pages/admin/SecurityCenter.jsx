import { useCallback, useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { securityApi } from '../../services/securityApi.js';

const fmt = (value) => value ? new Date(value).toLocaleString() : '—';
const defaultSessionFilters = { search: '', role: '' };

export default function SecurityCenter() {
  const { user: currentUser } = useAuth();
  const [overview, setOverview] = useState(null);
  const [events, setEvents] = useState([]);
  const [locked, setLocked] = useState([]);
  const [sessionResult, setSessionResult] = useState({ total: 0, sessions: [] });
  const [sessionForm, setSessionForm] = useState(defaultSessionFilters);
  const [sessionFilters, setSessionFilters] = useState(defaultSessionFilters);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busyId, setBusyId] = useState(null);
  const isSuperAdmin = currentUser?.role === 'SuperAdmin';

  const roleOptions = useMemo(() => [
    'Citizen', 'Officer', 'Supervisor', 'Admin', ...(isSuperAdmin ? ['SuperAdmin'] : []),
  ], [isSuperAdmin]);

  const load = useCallback(async () => {
    setError('');
    try {
      const [nextOverview, nextEvents, nextLocked, nextSessions] = await Promise.all([
        securityApi.getOverview(),
        securityApi.getEvents(75),
        securityApi.getLockedAccounts(),
        securityApi.getManagedSessions({
          search: sessionFilters.search || undefined,
          role: sessionFilters.role || undefined,
          take: 150,
        }),
      ]);
      setOverview(nextOverview);
      setEvents(nextEvents);
      setLocked(nextLocked);
      setSessionResult(nextSessions);
    } catch (reason) {
      setError(reason.message || 'Unable to load the security centre.');
    }
  }, [sessionFilters]);

  useEffect(() => { load(); }, [load]);

  const runAccountAction = async (id, action) => {
    setBusyId(`${action}-${id}`);
    setError('');
    setMessage('');
    try {
      const result = action === 'unlock'
        ? await securityApi.unlockAccount(id)
        : await securityApi.revokeUserSessions(id);
      setMessage(`${result.email}: ${result.action}.`);
      await load();
    } catch (reason) {
      setError(reason.message || 'Security action failed.');
    } finally {
      setBusyId(null);
    }
  };

  const applySessionFilters = (event) => {
    event.preventDefault();
    setSessionFilters({ search: sessionForm.search.trim(), role: sessionForm.role });
  };

  const clearSessionFilters = () => {
    setSessionForm(defaultSessionFilters);
    setSessionFilters(defaultSessionFilters);
  };

  const revokeSelectedSession = async (item) => {
    const warning = item.isCurrentActorSession
      ? 'This is your current administrator session. Revoking it will sign you out after this request.\n\nEnter the audited reason:'
      : `Revoke ${item.fullName}'s session on ${item.deviceLabel}?\n\nEnter the audited reason:`;
    const reason = window.prompt(warning, 'Security review confirmed this session should be revoked.');
    if (reason === null) return;
    if (reason.trim().length < 10 || reason.trim().length > 400) {
      setError('The revocation reason must contain 10–400 characters.');
      return;
    }
    if (!window.confirm(`Revoke the selected session for ${item.email}? Other active devices will remain signed in.`)) return;

    setBusyId(`session-${item.sessionId}`);
    setError('');
    setMessage('');
    try {
      const result = await securityApi.revokeManagedSession(item.sessionId, { reason: reason.trim() });
      setMessage(`${result.email}: ${result.deviceLabel} session revoked.`);
      await load();
    } catch (reasonError) {
      setError(reasonError.message || 'Unable to revoke the selected session.');
    } finally {
      setBusyId(null);
    }
  };

  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-400">Security operations</p>
        <h2 className="mt-2 text-3xl font-black text-white">Security centre</h2>
        <p className="mt-2 text-slate-400">Monitor authentication risk, lockouts, rejected traffic and individual active sessions.</p>
      </div>
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
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 className="text-xl font-bold text-white">Individual active-session governance</h3>
          <p className="mt-1 text-sm text-slate-400">Review one browser or device and revoke only that session. Other devices remain active.</p>
          {!isSuperAdmin && <p className="mt-1 text-xs text-amber-300">Protected Super Admin sessions are hidden from normal Admin accounts.</p>}
        </div>
        <span className="cv-status-blue">{sessionResult.total ?? 0} matching sessions</span>
      </div>

      <form onSubmit={applySessionFilters} className="mt-4 grid gap-3 md:grid-cols-[minmax(0,1fr)_220px_auto]">
        <input
          value={sessionForm.search}
          onChange={(event) => setSessionForm((current) => ({ ...current, search: event.target.value }))}
          placeholder="Search name, email, device, IP or session ID"
          className="input mt-0"
        />
        <select
          value={sessionForm.role}
          onChange={(event) => setSessionForm((current) => ({ ...current, role: event.target.value }))}
          className="input mt-0"
        >
          <option value="">All eligible roles</option>
          {roleOptions.map((role) => <option key={role} value={role}>{role}</option>)}
        </select>
        <div className="flex gap-2">
          <button type="submit" className="cv-btn-primary">Apply</button>
          <button type="button" onClick={clearSessionFilters} className="cv-btn-secondary">Clear</button>
        </div>
      </form>

      <div className="mt-4 overflow-x-auto">
        <table className="min-w-full text-left text-sm">
          <thead className="text-xs uppercase tracking-wider text-slate-500">
            <tr><th className="p-3">User</th><th className="p-3">Device</th><th className="p-3">Network</th><th className="p-3">Last activity</th><th className="p-3">Expires</th><th className="p-3">Action</th></tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {(sessionResult.sessions || []).length === 0
              ? <tr><td colSpan="6" className="p-6 text-center text-slate-400">No eligible active sessions match the filters.</td></tr>
              : sessionResult.sessions.map((item) => <tr key={item.sessionId}>
                <td className="p-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-semibold text-white">{item.fullName}</p>
                    <span className="rounded-full bg-white/10 px-2 py-0.5 text-[10px] font-bold text-slate-300">{item.role}</span>
                    {item.isCurrentActorSession && <span className="cv-status-amber !px-2 !py-0.5 !text-[10px]">Current admin session</span>}
                  </div>
                  <p className="mt-1 text-xs text-slate-500">{item.email}</p>
                  <p className="mt-1 max-w-[280px] truncate font-mono text-[10px] text-slate-600" title={item.sessionId}>{item.sessionId}</p>
                </td>
                <td className="p-3"><p className="font-medium text-slate-200">{item.deviceLabel}</p><p className="mt-1 max-w-[260px] truncate text-xs text-slate-500" title={item.userAgent || ''}>{item.userAgent || 'Unknown user agent'}</p></td>
                <td className="p-3 text-slate-300">{item.ipAddress || 'Unknown IP'}</td>
                <td className="p-3 text-slate-300"><p>{fmt(item.lastSeenAtUtc)}</p><p className="mt-1 text-xs text-slate-500">Created {fmt(item.createdAtUtc)}</p></td>
                <td className="p-3 text-slate-300">{fmt(item.expiresAtUtc)}</td>
                <td className="p-3"><button disabled={busyId === `session-${item.sessionId}`} onClick={() => revokeSelectedSession(item)} className="cv-btn-danger !min-h-0 !px-3 !py-2 !text-xs">{busyId === `session-${item.sessionId}` ? 'Revoking…' : 'Revoke selected'}</button></td>
              </tr>)}
          </tbody>
        </table>
      </div>
    </div>

    <div className="rounded-2xl border border-white/10 bg-white/5 p-5">
      <h3 className="text-xl font-bold text-white">Accounts requiring attention</h3>
      <div className="mt-4 overflow-x-auto">
        <table className="min-w-full text-left text-sm"><thead className="text-xs uppercase tracking-wider text-slate-500"><tr><th className="p-3">User</th><th className="p-3">Role</th><th className="p-3">Failed attempts</th><th className="p-3">Locked until</th><th className="p-3">Actions</th></tr></thead>
          <tbody className="divide-y divide-white/10">{locked.length === 0 ? <tr><td colSpan="5" className="p-5 text-center text-slate-400">No accounts currently require attention.</td></tr> : locked.map((user) => <tr key={user.id}>
            <td className="p-3"><p className="font-semibold text-white">{user.fullName}</p><p className="text-xs text-slate-500">{user.email}</p></td><td className="p-3 text-slate-300">{user.role}</td><td className="p-3 text-slate-300">{user.failedLoginAttempts}</td><td className="p-3 text-slate-300">{fmt(user.lockoutEndUtc)}</td><td className="p-3"><div className="flex gap-2"><button disabled={busyId === `unlock-${user.id}`} onClick={() => runAccountAction(user.id, 'unlock')} className="rounded-lg bg-emerald-500/20 px-3 py-2 text-xs font-bold text-emerald-200">Unlock</button><button disabled={busyId === `revoke-${user.id}`} onClick={() => runAccountAction(user.id, 'revoke')} className="rounded-lg bg-rose-500/20 px-3 py-2 text-xs font-bold text-rose-200">Revoke all sessions</button></div></td>
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

function Metric({ label, value, hint }) {
  return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 text-3xl font-black text-white">{value ?? '—'}</p><p className="mt-2 text-xs text-slate-400">{hint}</p></div>;
}
