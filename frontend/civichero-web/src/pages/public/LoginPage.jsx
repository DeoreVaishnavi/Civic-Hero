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

  return <section className="mx-auto max-w-xl px-6 py-14"><div className="rounded-3xl border border-white/10 bg-white/5 p-7 shadow-2xl">
    <p className="text-sm font-bold uppercase tracking-[0.2em] text-sky-300">Secure citizen access</p><h1 className="mt-2 text-3xl font-black text-white">Login to CivicHero</h1><p className="mt-2 text-slate-400">Use your email or verified phone number.</p>
    <div className="mt-6 grid grid-cols-2 rounded-xl bg-slate-900 p-1"><button type="button" onClick={() => setMode('password')} className={`rounded-lg px-3 py-2 font-bold ${mode === 'password' ? 'bg-sky-500 text-white' : 'text-slate-400'}`}>Password</button><button type="button" onClick={() => setMode('otp')} className={`rounded-lg px-3 py-2 font-bold ${mode === 'otp' ? 'bg-sky-500 text-white' : 'text-slate-400'}`}>Phone OTP</button></div>
    {error && <p className="mt-4 rounded-lg bg-rose-500/10 p-3 text-sm text-rose-200">{error}</p>}
    {mode === 'password' ? <form onSubmit={submitPassword} className="mt-6 space-y-4"><Field label="Email or verified phone"><input required value={form.identifier} onChange={(e) => setForm({ ...form, identifier: e.target.value })} autoComplete="username" className="input" /></Field><Field label="Password"><input required type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} autoComplete="current-password" className="input" /></Field><TwoFactor form={form} setForm={setForm} /><button disabled={submitting} className="w-full rounded-lg bg-sky-500 px-4 py-3 font-bold text-white disabled:opacity-60">{submitting ? 'Logging in…' : 'Login'}</button></form>
      : <form onSubmit={submitOtp} className="mt-6 space-y-4"><Field label="Verified phone number"><input required value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} autoComplete="tel" placeholder="+919876543210" className="input" /></Field><button type="button" onClick={requestOtp} disabled={submitting || !form.phoneNumber} className="w-full rounded-lg border border-sky-400/30 px-4 py-3 font-bold text-sky-200 disabled:opacity-50">Request one-time code</button>{otpInfo && <div className="rounded-xl border border-emerald-400/30 bg-emerald-400/10 p-3 text-sm text-emerald-100">Code sent to {otpInfo.maskedPhoneNumber}. Expires {new Date(otpInfo.expiresAtUtc).toLocaleTimeString()}.{otpInfo.developmentCode && <strong className="ml-2">Development code: {otpInfo.developmentCode}</strong>}</div>}<Field label="Six-digit OTP"><input required inputMode="numeric" pattern="[0-9]{6}" maxLength="6" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value.replace(/\D/g, '') })} autoComplete="one-time-code" className="input" /></Field><TwoFactor form={form} setForm={setForm} /><button disabled={submitting || !otpInfo} className="w-full rounded-lg bg-sky-500 px-4 py-3 font-bold text-white disabled:opacity-60">{submitting ? 'Verifying…' : 'Login with OTP'}</button></form>}
    <div className="mt-6 grid gap-3 border-t border-white/10 pt-5 text-center text-sm"><p className="text-slate-400">New citizen? <Link className="font-semibold text-sky-300" to={ROUTE_PATHS.register}>Create an account</Link></p><Link className="rounded-xl border border-emerald-400/30 px-4 py-3 font-bold text-emerald-200" to={ROUTE_PATHS.anonymousReport}>Continue anonymously to report an issue</Link><Link className="font-semibold text-slate-300" to={ROUTE_PATHS.anonymousTrack}>Track an anonymous complaint</Link></div>
  </div></section>;
}
function Field({ label, children }) { return <label className="block text-sm text-slate-300">{label}{children}</label>; }
function TwoFactor({ form, setForm }) { return <Field label="Authenticator or recovery code (2FA accounts only)"><input value={form.twoFactorCode} onChange={(e) => setForm({ ...form, twoFactorCode: e.target.value })} inputMode="numeric" autoComplete="one-time-code" className="input" /></Field>; }
