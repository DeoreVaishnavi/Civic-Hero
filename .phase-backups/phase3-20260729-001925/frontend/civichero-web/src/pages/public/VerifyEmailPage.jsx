import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { dashboardForRole } from '../../utils/roleRouting.js';

export default function VerifyEmailPage() {
  const location = useLocation(); const navigate = useNavigate(); const { verifyEmail } = useAuth();
  const [email, setEmail] = useState(location.state?.email || ''); const [token, setToken] = useState(location.state?.token || '');
  const [error, setError] = useState(null); const [submitting, setSubmitting] = useState(false);
  const submit = async (event) => { event.preventDefault(); setSubmitting(true); setError(null); try { const session = await verifyEmail({ email, token }); navigate(dashboardForRole(session.user?.role), { replace: true }); } catch (apiError) { setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message); } finally { setSubmitting(false); } };
  return <section className="mx-auto max-w-lg px-6 py-16"><div className="rounded-2xl border border-white/10 bg-white/5 p-7"><h1 className="text-3xl font-black text-white">Verify your email</h1><p className="mt-2 text-slate-400">Development mode returns the temporary token after registration.</p>{location.state?.message && <p className="mt-4 rounded-lg bg-emerald-500/10 p-3 text-sm text-emerald-200">{location.state.message}</p>}{error && <p className="mt-4 rounded-lg bg-rose-500/10 p-3 text-sm text-rose-200">{error}</p>}<form onSubmit={submit} className="mt-6 space-y-4"><label className="block text-sm text-slate-300">Email<input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="input" /></label><label className="block text-sm text-slate-300">Verification token<textarea required rows="3" value={token} onChange={(e) => setToken(e.target.value)} className="input font-mono text-sm" /></label><button disabled={submitting} className="w-full rounded-lg bg-sky-500 px-4 py-3 font-bold text-white disabled:opacity-60">{submitting ? 'Verifying…' : 'Verify and continue'}</button></form></div></section>;
}
