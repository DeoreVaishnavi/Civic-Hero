import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { dashboardForRole } from '../../utils/roleRouting.js';

const SLOW_LOGIN_HINT_DELAY_MS = 8_000;

function normalizeLoginError(apiError) {
  const message = apiError?.errors?.length
    ? apiError.errors.join(' ')
    : apiError?.message || 'Login failed. Please try again.';

  return {
    message,
    traceId: apiError?.traceId || null,
    canRetry: Boolean(apiError?.isTimeout || apiError?.isNetworkError || apiError?.status === 0),
  };
}

export default function LoginPage() {
  const [mode, setMode] = useState('password');
  const [form, setForm] = useState({ identifier: '', password: '', phoneNumber: '', code: '', twoFactorCode: '' });
  const [otpInfo, setOtpInfo] = useState(null);
  const [error, setError] = useState(null);
  const [progressMessage, setProgressMessage] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const { login, phoneLogin, requestPhoneLoginOtp } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const finish = (session) => navigate(location.state?.from || dashboardForRole(session.user?.role), { replace: true });

  const startProgressHint = (initialMessage) => {
    setProgressMessage(initialMessage);
    return globalThis.setTimeout(() => {
      setProgressMessage('Still connecting securely. The first AWS RDS request can take longer when the connection is cold.');
    }, SLOW_LOGIN_HINT_DELAY_MS);
  };

  const submitPassword = async (event) => {
    event.preventDefault();
    if (submitting) return;

    setSubmitting(true);
    setError(null);
    const hintTimer = startProgressHint('Checking your account securely…');

    try {
      const session = await login({
        identifier: form.identifier.trim(),
        password: form.password,
        twoFactorCode: form.twoFactorCode.trim(),
      });
      finish(session);
    } catch (apiError) {
      setError(normalizeLoginError(apiError));
    } finally {
      globalThis.clearTimeout(hintTimer);
      setProgressMessage('');
      setSubmitting(false);
    }
  };

  const requestOtp = async () => {
    if (submitting) return;

    setSubmitting(true);
    setError(null);
    const hintTimer = startProgressHint('Requesting a one-time code…');

    try {
      setOtpInfo(await requestPhoneLoginOtp(form.phoneNumber.trim()));
    } catch (apiError) {
      setError(normalizeLoginError(apiError));
    } finally {
      globalThis.clearTimeout(hintTimer);
      setProgressMessage('');
      setSubmitting(false);
    }
  };

  const submitOtp = async (event) => {
    event.preventDefault();
    if (submitting) return;

    setSubmitting(true);
    setError(null);
    const hintTimer = startProgressHint('Verifying the one-time code…');

    try {
      const session = await phoneLogin({
        phoneNumber: form.phoneNumber.trim(),
        code: form.code,
        twoFactorCode: form.twoFactorCode.trim(),
      });
      finish(session);
    } catch (apiError) {
      setError(normalizeLoginError(apiError));
    } finally {
      globalThis.clearTimeout(hintTimer);
      setProgressMessage('');
      setSubmitting(false);
    }
  };

  const switchMode = (nextMode) => {
    if (submitting) return;
    setMode(nextMode);
    setError(null);
    setProgressMessage('');
  };

  return (
    <section className="auth-shell">
      <aside className="auth-visual">
        <div className="auth-visual-content">
          <p className="section-kicker" style={{ color: '#83baf8' }}>Welcome back</p>
          <h1>Your city becomes better when every issue is visible.</h1>
          <p>Log in to report civic problems, track officer updates, verify completed work and earn rewards for meaningful participation.</p>
        </div>
        <div className="auth-benefits"><span>Transparent complaint timeline</span><span>Secure role-based access</span><span>Citizen verification and dispute support</span><span>Points, badges and community rank</span></div>
      </aside>

      <div className="auth-panel">
        <div className="auth-card">
          <p className="section-kicker">Secure access</p>
          <h1>Login to CivicHero</h1>
          <p>Use your email, verified phone number or phone OTP.</p>

          <div className="auth-tabs">
            <button type="button" disabled={submitting} onClick={() => switchMode('password')} className={`auth-tab ${mode === 'password' ? 'active' : ''}`}>Password</button>
            <button type="button" disabled={submitting} onClick={() => switchMode('otp')} className={`auth-tab ${mode === 'otp' ? 'active' : ''}`}>Phone OTP</button>
          </div>

          {error && (
            <div className="alert error">
              <strong>{error.message}</strong>
              {error.canRetry && <div style={{ marginTop: 6 }}>Keep the backend terminal running on port 5180, then submit again.</div>}
              {error.traceId && <div style={{ marginTop: 6, fontSize: 10 }}>Trace ID: {error.traceId}</div>}
            </div>
          )}

          {progressMessage && (
            <div className="alert" role="status" aria-live="polite" style={{ borderColor: '#93c5fd', background: '#eff6ff', color: '#1e3a8a' }}>
              {progressMessage}
            </div>
          )}

          {mode === 'password' ? (
            <form onSubmit={submitPassword}>
              <Field label="Email or verified phone" icon="○"><input required disabled={submitting} value={form.identifier} onChange={(e) => setForm({ ...form, identifier: e.target.value })} autoComplete="username" className="input" placeholder="you@example.com" /></Field>
              <Field label="Password" icon="◇"><input required disabled={submitting} type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} autoComplete="current-password" className="input" placeholder="Enter password" /></Field>
              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 9 }}><Link to="/forgot-password" style={{ color: 'var(--civic-blue)', fontSize: 10, fontWeight: 800 }}>Forgot password?</Link></div>
              <TwoFactor form={form} setForm={setForm} disabled={submitting} />
              <button disabled={submitting} className="button primary full large" style={{ marginTop: 20 }}>{submitting ? 'Signing in…' : 'Login securely'}</button>
            </form>
          ) : (
            <form onSubmit={submitOtp}>
              <Field label="Verified phone number" icon="▯"><input required disabled={submitting} value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} autoComplete="tel" placeholder="+919876543210" className="input" /></Field>
              <button type="button" onClick={requestOtp} disabled={submitting || !form.phoneNumber.trim()} className="button outline full" style={{ marginTop: 14 }}>Request one-time code</button>
              {otpInfo && <div className="alert success">Code sent to {otpInfo.maskedPhoneNumber}. Expires {new Date(otpInfo.expiresAtUtc).toLocaleTimeString()}.{otpInfo.developmentCode && <strong> Development code: {otpInfo.developmentCode}</strong>}</div>}
              <Field label="Six-digit OTP" icon="#"><input required disabled={submitting} inputMode="numeric" pattern="[0-9]{6}" maxLength="6" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value.replace(/\D/g, '') })} autoComplete="one-time-code" className="input" placeholder="000000" /></Field>
              <TwoFactor form={form} setForm={setForm} disabled={submitting} />
              <button disabled={submitting || !otpInfo} className="button primary full large" style={{ marginTop: 20 }}>{submitting ? 'Verifying…' : 'Login with OTP'}</button>
            </form>
          )}

          <div className="auth-divider">OR</div>
          <Link className="button outline full large" to={ROUTE_PATHS.anonymousReport}>◌ Continue as anonymous</Link>
          <p className="auth-note">Don’t have an account? <Link to={ROUTE_PATHS.register}>Create one now</Link></p>
          <p className="auth-note" style={{ marginTop: 8 }}><Link to={ROUTE_PATHS.anonymousTrack}>Track an anonymous complaint</Link></p>
        </div>
      </div>
    </section>
  );
}

function Field({ label, icon, children }) { return <label className="form-field">{label}<div className="form-icon-field"><span>{icon}</span>{children}</div></label>; }
function TwoFactor({ form, setForm, disabled }) { return <Field label="Authenticator or recovery code (2FA accounts only)" icon="✓"><input disabled={disabled} value={form.twoFactorCode} onChange={(e) => setForm({ ...form, twoFactorCode: e.target.value })} inputMode="numeric" autoComplete="one-time-code" className="input" placeholder="Optional for most citizen accounts" /></Field>; }
