import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { securityApi } from '../../services/securityApi.js';

const formatDate = (value) => value ? new Date(value).toLocaleString() : 'Not available';

export default function AccountSecurity() {
  const [session, setSession] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const { logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    securityApi.getSession().then(setSession).catch((reason) => setError(reason.message || 'Unable to load security information.'));
  }, []);

  const revoke = async () => {
    if (!window.confirm('Revoke every active session, including this browser?')) return;
    setBusy(true); setError('');
    try {
      await securityApi.revokeMySessions();
    } catch (reason) {
      setError(reason.message || 'Unable to revoke sessions.');
      setBusy(false);
      return;
    }
    try { await logout(); } finally { navigate('/login', { replace: true }); }
  };

  return <section className="mx-auto max-w-5xl space-y-6 p-6">
    <div>
      <p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-400">Account protection</p>
      <h2 className="mt-2 text-3xl font-black text-white">Security & sessions</h2>
      <p className="mt-2 text-slate-400">Review your current authentication session and immediately sign out every device when needed.</p>
    </div>

    {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-200">{error}</div>}
    {!session && !error && <div className="rounded-2xl border border-white/10 bg-white/5 p-6 text-slate-300">Loading secure session…</div>}

    {session && <>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {[
          ['Account', session.email], ['Role', session.role], ['Last login', formatDate(session.lastLoginAtUtc)],
          ['Access token expires', formatDate(session.accessTokenExpiresAtUtc)], ['Refresh session expires', formatDate(session.refreshSessionExpiresAtUtc)],
          ['Authorization version', session.authorizationVersion],
        ].map(([label, value]) => <div key={label} className="rounded-2xl border border-white/10 bg-white/5 p-5">
          <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p>
          <p className="mt-2 break-words font-semibold text-white">{value}</p>
        </div>)}
      </div>

      <div className="rounded-2xl border border-emerald-400/20 bg-emerald-400/5 p-6">
        <h3 className="text-lg font-bold text-white">Protection status</h3>
        <div className="mt-4 grid gap-3 sm:grid-cols-3">
          <Status label="Email verified" active={session.isEmailVerified} />
          <Status label="Account active" active={session.isActive} />
          <Status label="Refresh session" active={session.hasRefreshSession} />
        </div>
      </div>

      <div className="rounded-2xl border border-rose-400/20 bg-rose-400/5 p-6">
        <h3 className="text-lg font-bold text-white">Emergency sign-out</h3>
        <p className="mt-2 max-w-2xl text-sm text-slate-400">This invalidates the current access token and refresh session. Use it after losing a device or noticing suspicious activity.</p>
        <button disabled={busy} onClick={revoke} className="mt-5 rounded-xl bg-rose-500 px-5 py-3 font-bold text-white disabled:opacity-60">
          {busy ? 'Revoking sessions…' : 'Revoke all sessions'}
        </button>
      </div>
    </>}
  </section>;
}

function Status({ label, active }) {
  return <div className="rounded-xl border border-white/10 bg-slate-950/40 p-4">
    <span className={`mr-2 inline-block h-2.5 w-2.5 rounded-full ${active ? 'bg-emerald-400' : 'bg-amber-400'}`} />
    <span className="text-sm font-semibold text-slate-200">{label}</span>
  </div>;
}
