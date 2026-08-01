import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { securityApi } from '../../services/securityApi.js';

const formatDate = (value) => (value ? new Date(value).toLocaleString() : 'Not available');
const emptyPasswordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };

function ActivityStatus({ success }) {
  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${
      success ? 'bg-emerald-400/10 text-emerald-200' : 'bg-rose-400/10 text-rose-200'
    }`}>
      {success ? 'Successful' : 'Failed'}
    </span>
  );
}

export default function AccountSecurity() {
  const [session, setSession] = useState(null);
  const [sessions, setSessions] = useState({ supportsMultipleSessions: false, architectureNote: '', sessions: [] });
  const [activity, setActivity] = useState([]);
  const [twoFactor, setTwoFactor] = useState(null);
  const [phone, setPhone] = useState('');
  const [phoneCode, setPhoneCode] = useState('');
  const [phoneOtp, setPhoneOtp] = useState(null);
  const [setup, setSetup] = useState(null);
  const [code, setCode] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState([]);
  const [passwordForm, setPasswordForm] = useState(emptyPasswordForm);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState('');

  const { logout, user, requestPhoneVerification, verifyPhone, refreshUser } = useAuth();
  const navigate = useNavigate();

  const load = async () => {
    setError('');
    try {
      const [nextSession, nextTwoFactor, nextActivity, nextSessions] = await Promise.all([
        securityApi.getSession(),
        securityApi.getTwoFactorStatus(),
        securityApi.getMyActivity(25),
        securityApi.getMySessions(),
      ]);
      setSession(nextSession);
      setTwoFactor(nextTwoFactor);
      setActivity(Array.isArray(nextActivity) ? nextActivity : []);
      setSessions(nextSessions || { supportsMultipleSessions: false, architectureNote: '', sessions: [] });
    } catch (reason) {
      setError(reason.message || 'Unable to load security information.');
    }
  };

  useEffect(() => {
    load();
    setPhone(user?.phone || '');
  }, [user?.phone]);

  const finishAndSignOut = async (text) => {
    setMessage(text);
    globalThis.setTimeout(async () => {
      try {
        await logout();
      } finally {
        navigate('/login', { replace: true });
      }
    }, 1200);
  };

  const updatePasswordField = (field, value) => {
    setPasswordForm((current) => ({ ...current, [field]: value }));
  };

  const changePassword = async (event) => {
    event.preventDefault();
    setError('');
    setMessage('');

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setError('New password and confirmation password must match.');
      return;
    }

    setBusy('password');
    try {
      await securityApi.changePassword(passwordForm);
      setPasswordForm(emptyPasswordForm);
      await finishAndSignOut('Password changed. All sessions were revoked. Signing you out…');
    } catch (reason) {
      setError(reason.errors?.length ? reason.errors.join(' ') : reason.message);
      setBusy('');
    }
  };

  const beginSetup = async () => {
    setBusy('setup');
    setError('');
    try {
      setSetup(await securityApi.beginTwoFactorSetup());
      setMessage('Add the manual key to Google Authenticator, Microsoft Authenticator or another TOTP app.');
    } catch (reason) {
      setError(reason.message);
    } finally {
      setBusy('');
    }
  };

  const enable = async () => {
    setBusy('enable');
    setError('');
    try {
      const result = await securityApi.enableTwoFactor(code);
      setRecoveryCodes(result.recoveryCodes || []);
      setTwoFactor({ enabled: true });
      setSetup(null);
      setCode('');
      setMessage('2FA enabled. Save the recovery codes, then sign in again.');
    } catch (reason) {
      setError(reason.message);
    } finally {
      setBusy('');
    }
  };

  const disable = async () => {
    setBusy('disable');
    setError('');
    try {
      await securityApi.disableTwoFactor(code);
      await finishAndSignOut('2FA disabled. Signing you out…');
    } catch (reason) {
      setError(reason.message);
      setBusy('');
    }
  };

  const requestPhoneCode = async () => {
    setBusy('phone-request');
    setError('');
    try {
      setPhoneOtp(await requestPhoneVerification(phone));
      setMessage('Verification code sent.');
    } catch (reason) {
      setError(reason.message);
    } finally {
      setBusy('');
    }
  };

  const confirmPhone = async () => {
    setBusy('phone-confirm');
    setError('');
    try {
      await verifyPhone({ phoneNumber: phone, code: phoneCode });
      await refreshUser();
      setPhoneCode('');
      setPhoneOtp(null);
      setMessage('Phone number verified. You can now use phone OTP login.');
    } catch (reason) {
      setError(reason.message);
    } finally {
      setBusy('');
    }
  };

  const revokeSelected = async (sessionId) => {
    if (!window.confirm('Revoke this stored session? You will be signed out.')) return;
    setBusy(`session-${sessionId}`);
    setError('');
    try {
      await securityApi.revokeMySession(sessionId);
      await finishAndSignOut('Selected session revoked. Signing out…');
    } catch (reason) {
      setError(reason.message);
      setBusy('');
    }
  };

  const revoke = async () => {
    if (!window.confirm('Revoke every active session, including this browser?')) return;
    setBusy('revoke');
    setError('');
    try {
      await securityApi.revokeMySessions();
      await finishAndSignOut('Sessions revoked. Signing out…');
    } catch (reason) {
      setError(reason.message);
      setBusy('');
    }
  };

  return (
    <section className="mx-auto max-w-5xl space-y-6 p-6">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-400">Account protection</p>
        <h2 className="mt-2 text-3xl font-black text-white">Security & sessions</h2>
        <p className="mt-2 text-slate-400">
          Change your password, review recent account activity, and manage authentication protection.
        </p>
      </div>

      {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-200">{error}</div>}
      {message && <div className="rounded-xl border border-emerald-400/30 bg-emerald-400/10 p-4 text-emerald-200">{message}</div>}

      {session && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {[
            ['Account', session.email],
            ['Role', session.role],
            ['Last login', formatDate(session.lastLoginAtUtc)],
            ['Access token expires', formatDate(session.accessTokenExpiresAtUtc)],
            ['Refresh session expires', formatDate(session.refreshSessionExpiresAtUtc)],
            ['Authorization version', session.authorizationVersion],
          ].map(([label, value]) => (
            <div key={label} className="rounded-2xl border border-white/10 bg-white/5 p-5">
              <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p>
              <p className="mt-2 break-words font-semibold text-white">{value}</p>
            </div>
          ))}
        </div>
      )}

      <form onSubmit={changePassword} className="rounded-2xl border border-violet-400/20 bg-violet-400/5 p-6">
        <h3 className="text-lg font-bold text-white">Change password</h3>
        <p className="mt-2 text-sm text-slate-400">
          Enter your current password and choose a new one. A successful change signs out every session.
        </p>
        <div className="mt-5 grid gap-4 md:grid-cols-3">
          <label className="space-y-2 text-sm font-semibold text-slate-300">
            Current password
            <input
              className="input"
              type="password"
              autoComplete="current-password"
              value={passwordForm.currentPassword}
              onChange={(event) => updatePasswordField('currentPassword', event.target.value)}
              required
            />
          </label>
          <label className="space-y-2 text-sm font-semibold text-slate-300">
            New password
            <input
              className="input"
              type="password"
              autoComplete="new-password"
              value={passwordForm.newPassword}
              onChange={(event) => updatePasswordField('newPassword', event.target.value)}
              minLength="8"
              maxLength="128"
              required
            />
          </label>
          <label className="space-y-2 text-sm font-semibold text-slate-300">
            Confirm new password
            <input
              className="input"
              type="password"
              autoComplete="new-password"
              value={passwordForm.confirmPassword}
              onChange={(event) => updatePasswordField('confirmPassword', event.target.value)}
              minLength="8"
              maxLength="128"
              required
            />
          </label>
        </div>
        <p className="mt-4 text-xs text-slate-500">
          Use 8–128 characters with uppercase, lowercase, number, and special character.
        </p>
        <button
          type="submit"
          disabled={Boolean(busy)}
          className="mt-5 rounded-xl bg-violet-500 px-5 py-3 font-black text-white disabled:opacity-50"
        >
          {busy === 'password' ? 'Changing password…' : 'Change password'}
        </button>
      </form>

      <div className="rounded-2xl border border-emerald-400/20 bg-emerald-400/5 p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h3 className="text-lg font-bold text-white">Verified phone & OTP login</h3>
            <p className="mt-2 text-sm text-slate-400">
              Status: <strong className="text-white">{user?.isPhoneVerified ? 'Verified' : 'Not verified'}</strong>.
              A verified number can be used for password recovery or one-time-code login.
            </p>
          </div>
        </div>
        <div className="mt-5 grid gap-3 md:grid-cols-[1fr_auto]">
          <input className="input" value={phone} onChange={(event) => setPhone(event.target.value)} placeholder="+919876543210" />
          <button
            type="button"
            disabled={Boolean(busy) || !phone}
            onClick={requestPhoneCode}
            className="rounded-xl border border-emerald-400/30 px-5 py-3 font-bold text-emerald-200 disabled:opacity-50"
          >
            Send verification code
          </button>
        </div>
        {phoneOtp && (
          <div className="mt-4 rounded-xl border border-emerald-400/20 bg-slate-950/40 p-4 text-sm text-emerald-100">
            Code sent to {phoneOtp.maskedPhoneNumber}. Expires {new Date(phoneOtp.expiresAtUtc).toLocaleTimeString()}.
            {phoneOtp.developmentCode && <strong className="ml-2">Development code: {phoneOtp.developmentCode}</strong>}
            <div className="mt-3 flex flex-wrap gap-3">
              <input
                className="input max-w-xs"
                value={phoneCode}
                onChange={(event) => setPhoneCode(event.target.value.replace(/\D/g, ''))}
                maxLength="6"
                inputMode="numeric"
                placeholder="6-digit code"
              />
              <button
                type="button"
                disabled={Boolean(busy) || phoneCode.length !== 6}
                onClick={confirmPhone}
                className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-white disabled:opacity-50"
              >
                Verify phone
              </button>
            </div>
          </div>
        )}
      </div>

      <div className="rounded-2xl border border-sky-400/20 bg-sky-400/5 p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="text-lg font-bold text-white">Authenticator two-factor authentication</h3>
            <p className="mt-2 text-sm text-slate-400">
              Status: <strong className="text-white">{twoFactor?.enabled ? 'Enabled' : twoFactor?.setupPending ? 'Setup pending' : 'Disabled'}</strong>
            </p>
          </div>
          {!twoFactor?.enabled && (
            <button type="button" disabled={Boolean(busy)} onClick={beginSetup} className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white">
              Start setup
            </button>
          )}
        </div>
        {setup && (
          <div className="mt-5 rounded-xl border border-white/10 bg-slate-950/50 p-4">
            <p className="text-sm text-slate-400">Manual entry key</p>
            <code className="mt-2 block break-all text-lg font-bold tracking-widest text-sky-200">{setup.manualEntryKey}</code>
            <p className="mt-3 break-all text-xs text-slate-500">{setup.otpAuthUri}</p>
          </div>
        )}
        {(setup || twoFactor?.enabled) && (
          <div className="mt-5 flex flex-wrap gap-3">
            <input value={code} onChange={(event) => setCode(event.target.value)} placeholder="6-digit or recovery code" className="input max-w-xs" />
            {setup && (
              <button type="button" disabled={Boolean(busy) || !code} onClick={enable} className="rounded-xl bg-emerald-500 px-5 py-3 font-bold text-white">
                Confirm and enable
              </button>
            )}
            {twoFactor?.enabled && (
              <button type="button" disabled={Boolean(busy) || !code} onClick={disable} className="rounded-xl bg-rose-500 px-5 py-3 font-bold text-white">
                Disable 2FA
              </button>
            )}
          </div>
        )}
        {recoveryCodes.length > 0 && (
          <div className="mt-5 rounded-xl border border-amber-400/20 bg-amber-400/5 p-4">
            <p className="font-bold text-amber-200">Save these one-time recovery codes now</p>
            <div className="mt-3 grid gap-2 sm:grid-cols-2">
              {recoveryCodes.map((item) => <code key={item} className="rounded bg-slate-950/70 p-2 text-center text-white">{item}</code>)}
            </div>
          </div>
        )}
      </div>

      <div className="rounded-2xl border border-white/10 bg-white/5 p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="text-lg font-bold text-white">Recent account activity</h3>
            <p className="mt-2 text-sm text-slate-400">Your latest authenticated account and application actions.</p>
          </div>
          <button type="button" disabled={Boolean(busy)} onClick={load} className="rounded-xl border border-white/15 px-4 py-2 text-sm font-bold text-slate-200 disabled:opacity-50">
            Refresh activity
          </button>
        </div>
        <div className="mt-5 space-y-3">
          {activity.length === 0 && (
            <div className="rounded-xl border border-dashed border-white/10 p-5 text-sm text-slate-500">No recorded activity is available yet.</div>
          )}
          {activity.map((item) => (
            <article key={item.id} className="rounded-xl border border-white/10 bg-slate-950/40 p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="font-bold text-white">{item.entityName} · {item.action}</p>
                  <p className="mt-1 text-xs text-slate-500">{formatDate(item.createdAtUtc)} · HTTP {item.httpStatusCode || '—'}</p>
                </div>
                <ActivityStatus success={item.success} />
              </div>
              <div className="mt-3 grid gap-2 text-xs text-slate-400 md:grid-cols-2">
                <p><span className="font-semibold text-slate-300">IP:</span> {item.ipAddress || 'Not recorded'}</p>
                <p className="break-all"><span className="font-semibold text-slate-300">Reference:</span> {item.correlationId || 'Not recorded'}</p>
              </div>
              {item.errorMessage && <p className="mt-3 text-sm text-rose-200">{item.errorMessage}</p>}
            </article>
          ))}
        </div>
      </div>


      <div className="rounded-2xl border border-cyan-400/20 bg-cyan-400/5 p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="text-lg font-bold text-white">Active login sessions</h3>
            <p className="mt-2 text-sm text-slate-400">Review the refresh session currently stored for your account and revoke it individually.</p>
          </div>
          <button type="button" disabled={Boolean(busy)} onClick={load} className="rounded-xl border border-cyan-400/30 px-4 py-2 text-sm font-bold text-cyan-100 disabled:opacity-50">
            Refresh sessions
          </button>
        </div>
        {sessions.architectureNote && (
          <p className="mt-4 rounded-xl border border-amber-400/20 bg-amber-400/5 p-3 text-xs text-amber-100">{sessions.architectureNote}</p>
        )}
        <div className="mt-5 space-y-3">
          {(sessions.sessions || []).length === 0 && (
            <div className="rounded-xl border border-dashed border-white/10 p-5 text-sm text-slate-500">No active refresh session is stored.</div>
          )}
          {(sessions.sessions || []).map((item) => (
            <article key={item.sessionId} className="rounded-xl border border-white/10 bg-slate-950/40 p-4">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="font-bold text-white">{item.deviceLabel}</p>
                  <p className="mt-1 text-xs text-slate-500">{item.sessionKind} · Session {item.sessionId}</p>
                  <div className="mt-3 grid gap-2 text-xs text-slate-400 sm:grid-cols-2">
                    <p><span className="font-semibold text-slate-300">Created:</span> {formatDate(item.createdAtUtc)}</p>
                    <p><span className="font-semibold text-slate-300">Expires:</span> {formatDate(item.expiresAtUtc)}</p>
                    <p><span className="font-semibold text-slate-300">Last login:</span> {formatDate(item.lastLoginAtUtc)}</p>
                    <p><span className="font-semibold text-slate-300">IP:</span> {item.ipAddress || 'Not recorded'}</p>
                  </div>
                  {item.userAgent && <p className="mt-3 break-all text-xs text-slate-500">{item.userAgent}</p>}
                </div>
                <button
                  type="button"
                  disabled={Boolean(busy)}
                  onClick={() => revokeSelected(item.sessionId)}
                  className="rounded-xl border border-rose-400/30 px-4 py-2 text-sm font-bold text-rose-200 disabled:opacity-50"
                >
                  {busy === `session-${item.sessionId}` ? 'Revoking…' : 'Revoke this session'}
                </button>
              </div>
            </article>
          ))}
        </div>
      </div>

      <div className="rounded-2xl border border-rose-400/20 bg-rose-400/5 p-6">
        <h3 className="text-lg font-bold text-white">Emergency sign-out</h3>
        <p className="mt-2 text-sm text-slate-400">Invalidate the current access token and refresh session on every device.</p>
        <button
          type="button"
          disabled={Boolean(busy)}
          onClick={revoke}
          className="mt-5 rounded-xl bg-rose-500 px-5 py-3 font-bold text-white disabled:opacity-60"
        >
          Revoke all sessions
        </button>
      </div>
    </section>
  );
}
