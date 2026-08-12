import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

export default function LoginPage() {
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const submit = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await login(form);
      navigate(location.state?.from || ROUTE_PATHS.citizenDashboard, { replace: true });
    } catch (apiError) {
      setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="mx-auto max-w-lg px-6 py-16">
      <div className="rounded-2xl border border-white/10 bg-white/5 p-7">
        <h1 className="text-3xl font-black text-white">Login to CivicHero</h1>
        <p className="mt-2 text-slate-400">Your refresh token is protected in an HttpOnly cookie.</p>
        {error && <p className="mt-4 rounded-lg bg-rose-500/10 p-3 text-sm text-rose-200">{error}</p>}
        <form onSubmit={submit} className="mt-6 space-y-4">
          <label className="block text-sm text-slate-300">Email
            <input required type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} className="mt-1 w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-3 text-white" />
          </label>
          <label className="block text-sm text-slate-300">Password
            <input required type="password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} className="mt-1 w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-3 text-white" />
          </label>
          <button disabled={submitting} className="w-full rounded-lg bg-sky-500 px-4 py-3 font-bold text-white disabled:opacity-60">
            {submitting ? 'Logging in…' : 'Login'}
          </button>
        </form>
        <p className="mt-5 text-center text-sm text-slate-400">
          New citizen? <Link className="font-semibold text-sky-300" to={ROUTE_PATHS.register}>Create an account</Link>
        </p>
      </div>
    </section>
  );
}
