import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { dashboardForRole } from '../../utils/roleRouting.js';

export default function LoginPage() {
  const [mode, setMode] = useState('password');
  const [form, setForm] = useState({ identifier: '', password: '', phoneNumber: '', code: '', twoFactorCode: '' });
  const [otpInfo, setOtpInfo] = useState(null);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const { login, phoneLogin, requestPhoneLoginOtp } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const finish = (session) => navigate(location.state?.from || dashboardForRole(session.user?.role), { replace: true });

  const submitPassword = async (event) => {
    event.preventDefault(); setSubmitting(true); setError(null);
    try { finish(await login({ identifier: form.identifier, password: form.password, twoFactorCode: form.twoFactorCode })); }
    catch (apiError) { setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message); }
    finally { setSubmitting(false); }
  };
  const requestOtp = async () => {
    setSubmitting(true); setError(null);
    try { setOtpInfo(await requestPhoneLoginOtp(form.phoneNumber)); }
    catch (apiError) { setError(apiError.errors?.join(' ') || apiError.message); }
    finally { setSubmitting(false); }
  };
  const submitOtp = async (event) => {
    event.preventDefault(); setSubmitting(true); setError(null);
    try { finish(await phoneLogin({ phoneNumber: form.phoneNumber, code: form.code, twoFactorCode: form.twoFactorCode })); }
    catch (apiError) { setError(apiError.errors?.join(' ') || apiError.message); }
    finally { setSubmitting(false); }
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
            <button type="button" onClick={() => setMode('password')} className={`auth-tab ${mode === 'password' ? 'active' : ''}`}>Password</button>
            <button type="button" onClick={() => setMode('otp')} className={`auth-tab ${mode === 'otp' ? 'active' : ''}`}>Phone OTP</button>
          </div>

          {error && <div className="alert error">{error}</div>}

          {mode === 'password' ? (
            <form onSubmit={submitPassword}>
              <Field label="Email or verified phone" icon="○"><input required value={form.identifier} onChange={(e) => setForm({ ...form, identifier: e.target.value })} autoComplete="username" className="input" placeholder="you@example.com" /></Field>
              <Field label="Password" icon="◇"><input required type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} autoComplete="current-password" className="input" placeholder="Enter password" /></Field>
              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 9 }}><Link to="/forgot-password" style={{ color: 'var(--civic-blue)', fontSize: 10, fontWeight: 800 }}>Forgot password?</Link></div>
              <TwoFactor form={form} setForm={setForm} />
              <button disabled={submitting} className="button primary full large" style={{ marginTop: 20 }}>{submitting ? 'Logging in…' : 'Login securely'}</button>
            </form>
          ) : (
            <form onSubmit={submitOtp}>
              <Field label="Verified phone number" icon="▯"><input required value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} autoComplete="tel" placeholder="+919876543210" className="input" /></Field>
              <button type="button" onClick={requestOtp} disabled={submitting || !form.phoneNumber} className="button outline full" style={{ marginTop: 14 }}>Request one-time code</button>
              {otpInfo && <div className="alert success">Code sent to {otpInfo.maskedPhoneNumber}. Expires {new Date(otpInfo.expiresAtUtc).toLocaleTimeString()}.{otpInfo.developmentCode && <strong> Development code: {otpInfo.developmentCode}</strong>}</div>}
              <Field label="Six-digit OTP" icon="#"><input required inputMode="numeric" pattern="[0-9]{6}" maxLength="6" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value.replace(/\D/g, '') })} autoComplete="one-time-code" className="input" placeholder="000000" /></Field>
              <TwoFactor form={form} setForm={setForm} />
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
function TwoFactor({ form, setForm }) { return <Field label="Authenticator or recovery code (2FA accounts only)" icon="✓"><input value={form.twoFactorCode} onChange={(e) => setForm({ ...form, twoFactorCode: e.target.value })} inputMode="numeric" autoComplete="one-time-code" className="input" placeholder="Optional for most citizen accounts" /></Field>; }
